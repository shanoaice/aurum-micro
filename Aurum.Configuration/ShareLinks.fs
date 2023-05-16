﻿module Aurum.Configuration.ShareLinks

open System.Collections.Generic
open Microsoft.AspNetCore.WebUtilities
open Aurum
open Aurum.Configuration.Shared.Adapter
open Aurum.Configuration.Shared.Shadowsocks
open Aurum.Configuration.Shared.V2fly

let createV2FlyObjectFromUri (uriObject: System.Uri) =
  let protocol = uriObject.Scheme
  let uuid = uriObject.UserInfo
  let host = uriObject.Host
  let port = uriObject.Port

  let description =
    if uriObject.Fragment.Length = 0 then
      ""
    else
      uriObject.Fragment.Substring(1)

  let queryParams = QueryHelpers.ParseQuery uriObject.Query

  let retrieveFromShareLink = getFirstQuerystringEntry queryParams

  let tryRetrieveFromShareLink = tryRetrieveFromShareLink queryParams

  let transportType = retrieveFromShareLink "type"

  let securityType = tryRetrieveFromShareLink "security" |> Option.defaultValue "none"

  let transportSetting =
    match transportType with
    | "ws" ->
      createWebSocketObject (
        (tryRetrieveFromShareLink "path"),
        None,
        None,
        None,
        (tryRetrieveFromShareLink "host"),
        None
      )
    | "grpc" -> retrieveFromShareLink "serviceName" |> createGrpcObject
    | "http" -> createHttpObject (tryRetrieveFromShareLink "path", tryRetrieveFromShareLink "host", Dictionary())
    | "quic" -> createQuicObject ()
    | "kcp" -> createKCPObject (None, None, None, None, None, None, None, (tryRetrieveFromShareLink "seed"))
    | "tcp" -> createTCPObject ()
    | unknown -> raise (ConfigurationParameterException $"unknown transport protocol {unknown}")

  let protocolSetting =
    match protocol with
    | "vmess" -> createVMessObject (host, port, uuid, VMessSecurity.Auto)
    | unknown -> raise (ShareLinkFormatException $"unknown sharelink protocol {unknown}")

  let securitySetting =
    match securityType with
    | "tls" ->
      createTLSObject (
        tryRetrieveFromShareLink "sni",
        tryRetrieveFromShareLink "alpn"
        |> Option.map (fun alpn -> alpn.Split(",") |> Seq.toList),
        Some false
      )
    | "none" -> TransportSecurity.None
    | unsupported -> raise (ShareLinkFormatException $"unsupported security type {unsupported}")

  createConfigurationEntry (description, V2fly(createV2flyObject protocolSetting transportSetting securitySetting))

let createShadowsocksObjectFromUri (uriObject: System.Uri) =
  let host = uriObject.Host
  let port = uriObject.Port

  let queryParams = QueryHelpers.ParseQuery uriObject.Query

  let tryRetrieveFromShareLink = tryRetrieveFromShareLink queryParams

  let description =
    if uriObject.Fragment.Length = 0 then
      ""
    else
      uriObject.Fragment.Substring(1)

  let protocolString, encryptionInfo =
    match
      (if uriObject.UserInfo.IndexOf(":") <> -1 then
         Array.toList (System.Uri.UnescapeDataString(uriObject.UserInfo).Split(":"))
       else
         Array.toList ((decodeBase64Url uriObject.UserInfo).Split(":")))
    with
    | protocol :: info -> protocol, info
    | _ -> raise (ShareLinkFormatException $"ill-formed user info \"{uriObject.UserInfo}\"")

  let method =
    match protocolString with
    | "none" -> ShadowsocksEncryption.None
    | "plain" -> ShadowsocksEncryption.Plain
    | "chacha20-poly1305" -> ShadowsocksEncryption.ChaCha20 encryptionInfo.Head
    | "chacha20-ietf-poly1305" -> ShadowsocksEncryption.ChaCha20Ietf encryptionInfo.Head
    | "aes-128-gcm" -> ShadowsocksEncryption.AES128 encryptionInfo.Head
    | "aes-256-gcm" -> ShadowsocksEncryption.AES256 encryptionInfo.Head
    | "2022-blake3-aes-128-gcm" -> ShadowsocksEncryption.AES128_2022 encryptionInfo
    | "2022-blake3-aes-256-gcm" -> ShadowsocksEncryption.AES256_2022 encryptionInfo
    | "2022-blake3-chacha20-poly1305" -> ShadowsocksEncryption.ChaCha20_2022 encryptionInfo
    | "2022-blake3-chacha8-poly1305" -> ShadowsocksEncryption.ChaCha8_2022 encryptionInfo
    | method -> raise (ShareLinkFormatException $"unknown Shadowsocks encryption method {method}")

  let plugin =
    tryRetrieveFromShareLink "plugin"
    |> Option.map (fun op ->
      let pluginOpt = op.Split ";" |> Array.toList

      match pluginOpt with
      | "obfs" :: opts -> SimpleObfs(System.String.Join(",", List.toArray opts))
      | "v2ray" :: opts -> V2ray(System.String.Join(",", List.toArray opts))
      | pluginName :: _ -> raise (ShareLinkFormatException $"unknown plugin {pluginName}")
      | unknown -> raise (ShareLinkFormatException $"ill-formed plugin option \"{unknown}\""))

  createConfigurationEntry (description, Shadowsocks(createShadowsocksObject (host, port, method, plugin)))

let decodeShareLink link =
  let uriObject = System.Uri link

  match uriObject.Scheme with
  | "vmess"
  | "vless" -> createV2FlyObjectFromUri uriObject
  | "ss" -> createShadowsocksObjectFromUri uriObject
  | unknown -> raise (ShareLinkFormatException $"unsupported sharelink protocol {unknown}")