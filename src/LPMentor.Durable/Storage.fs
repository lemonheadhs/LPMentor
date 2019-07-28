module LPMentor.Durable.Storage

open System
open Microsoft.Azure.Storage
open System.Collections.Concurrent
open Newtonsoft.Json

module Blob = begin
    open Microsoft.Azure.Storage.Blob

    let private containerKey = "AudiosBlobContainerKey"
    let private containerCache = ConcurrentDictionary<string, CloudBlobContainer>()

    let private buildContainer ignored = 
        let connStr = Environment.GetEnvironmentVariable("connStr")
        CloudStorageAccount.Parse(connStr)
                           .CreateCloudBlobClient()
                           .GetContainerReference("audios")

    let getAudioContainer () =
        containerCache.GetOrAdd (containerKey, buildContainer)

end

module Table = begin
    open LPMentor.Core
    open LPMentor.Core.TableQuery
    open LPMentor.Core.Models
    open Microsoft.Azure.Cosmos.Table
    open LPMentor.Core.WebhookFn.EvernoteAuth
    open FSharp.Control.Tasks.V2
    open System.Threading.Tasks

    let private audioTableKey = "AudioInfoTableKey"
    let private credTableKey = "CredTableKey"
    let private cataTableKey = "CataTableKey"
    let private tableCache = ConcurrentDictionary<string, CloudTable>()

    let private buildTable s (ignored:string) =
        let connStr = Environment.GetEnvironmentVariable("connStr")
        CloudStorageAccount.Parse(connStr)
                           .CreateCloudTableClient()
                           .GetTableReference(s)

    let private buildAudioTable   = buildTable "LPAudio"
    let private buildCredTable    = buildTable "LPCredential"
    let private buildCatalogTable = buildTable "LPCatalog"

    let getAudioInfoTable () =
        tableCache.GetOrAdd (audioTableKey, buildAudioTable)

    let getCredentialTable () =
        tableCache.GetOrAdd (credTableKey, buildCredTable)

    let getCatalogTable () =
        tableCache.GetOrAdd (cataTableKey, buildCatalogTable)

    type AudioEntity (partition:string, rowId:string) =
        inherit TableEntity (partition, rowId)
        new() = AudioEntity("","")
        member val Text = "" with get, set
        member val Topic = "" with get, set
        member val Section = "" with get, set
        member val Order = Int32.MaxValue with get, set
        member val Lang = "en" with get, set
        member val BlobName = "" with get, set
    with
        static member ModifyWith (ni: NoteInfo, audioBlobName: string) (e: AudioEntity) =
            e.BlobName <- audioBlobName
            e.Text <- ni.Text.Substring(0, Math.Min(ni.Text.Length, 1000))
            e.Topic <- ni.Topic
            e.Section <- ni.Section
            e.Order <- ni.Order
            e.Lang <- ni.Lang
            e
        static member Save (ni: NoteInfo, audioBlobName: string) =
            let e = 
                AudioEntity("v1", sprintf "%s_%i_%s" ni.Topic ni.Order ni.Section)
                |> AudioEntity.ModifyWith (ni, audioBlobName)
            let table = getAudioInfoTable ()
            e
            |> TableOperation.InsertOrReplace
            |> table.ExecuteAsync
        static member SearchByTopic (topic:string) =
            let table = getAudioInfoTable ()
            TableQuery<AudioEntity>()
                .Where(
                        ("PartitionKey" == "v1")
                      + ("RowKey" ^>= topic)
                      + ("RowKey" ^< (topic+EndingChar)))
            |> table.ExecuteQuery

    type CredEntity (partition:string, rowId:string) =
        inherit TableEntity (partition, rowId)
        new() = CredEntity("","")
        member val Host = "" with get, set
        member val EdamUserId = "" with get, set
        member val NoteStoreUrl = "" with get, set
        member val WebApiUrlPrefix = "" with get, set
        member val AuthenticationToken = "" with get, set
        member val ExpirationDate = DateTime() with get, set
    with
        static member ModifyWith (ni: LENCredentials) (e: CredEntity) =
            e.Host <- ni.Host
            e.EdamUserId <- ni.EdamUserId
            e.NoteStoreUrl <- ni.NoteStoreUrl
            e.WebApiUrlPrefix <- ni.WebApiUrlPrefix
            e.AuthenticationToken <- ni.AuthenticationToken
            e.ExpirationDate <- ni.ExpirationDate
            e
        static member Save (ni: LENCredentials) =
            let e = 
                CredEntity("v1", SessionHost)
                |> CredEntity.ModifyWith (ni)
            let table = getCredentialTable ()
            e
            |> TableOperation.InsertOrReplace
            |> table.ExecuteAsync
        static member TryGet () =
            let table = getCredentialTable ()
            task {
                let! result =
                    TableOperation.Retrieve<CredEntity>("v1", SessionHost)
                    |> table.ExecuteAsync
                    
                return    
                    if result.Result |> isNull then None
                    else result.Result :?> CredEntity |> Some
            }

    [<CLIMutable>]
    type CatalogPiece = {
        section:string
        url:string
        order:int
    }
    
    type CatalogEntity (partition:string, rowId:string) =
        inherit TableEntity (partition, rowId)
        new() = CatalogEntity("","")
        member val Summary = "" with get, set
    with
        static member Recollect (topic:string) =
            AudioEntity.SearchByTopic topic
            |> Seq.map (fun x -> 
                            { section = x.Section
                              order = x.Order
                              url = x.BlobName })
            |> Seq.toArray
            |> fun ls ->
                let c = CatalogEntity ("v1", topic)
                c.Summary <- JsonConvert.SerializeObject ls
                c
        static member Save (e: CatalogEntity) =
            let table = getCatalogTable ()
            e 
            |> TableOperation.InsertOrReplace
            |> table.ExecuteAsync
end
