﻿module Aurum.Storage.Configuration

open FSharp.Json
open SQLite
open SQLite
open SQLitePCL

type ConnectionRecordObject =
    { Id: string
      Name: string
      Tags: string list }

type SubscriptionType =
    | None = 0
    | Base64 = 1 // not suggested
    | SIP008 = 2
    | OOCv1 = 3

type SubscriptionObject =
    { [<JsonField("type")>]
      SubscriptionType: SubscriptionType
      Source: string option }

type GroupObject =
    { Id: string
      Name: string
      Subscription: SubscriptionType
      SubscriptionSource: string option
      Connections: string list (* stores id of connections belong to this group *)  }

[<Table("Tags")>]
type Tags(tag: string, nodeId: string) =
    [<PrimaryKey>]
    [<AutoIncrement>]
    [<Column("tag")>]
    member this.Tag = tag

    [<Column("nodeId")>]
    member this.NodeId = nodeId

[<Table("Connections")>]
type Connections(name: string, id: string, configuration: string, connectionType: string, host: string, port: string) =
    [<PrimaryKey>]
    [<AutoIncrement>]
    [<Column("id")>]
    member this.Id = id

    [<Column("name")>]
    member this.Name = name

    [<Column("configuration")>]
    member this.Configuration = configuration

    [<Column("type")>]
    member this.Type = connectionType

    [<Column("host")>]
    member this.Host = host

    [<Column("port")>]
    member this.Port = port

[<Table("Groups")>]
type Groups(name: string, id: string, connectionId: string, subType: SubscriptionType, subUrl: string) =
    [<PrimaryKey>]
    [<AutoIncrement>]
    [<Column("name")>]
    member this.Name = name

    [<Column("id")>]
    member this.Id = id

    [<Column("connectionId")>]
    member this.connectionId = connectionId

    [<Column("subscriptionType")>]
    member this.Type = subType

    [<Column("subscriptionUrl")>]
    member this.Url = subUrl