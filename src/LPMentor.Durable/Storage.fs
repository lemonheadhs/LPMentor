module LPMentor.Durable.Storage

open System
open Microsoft.Azure.Storage
open System.Collections.Concurrent

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
    open LPMentor.Core.Models
    open Microsoft.Azure.Cosmos.Table
    open LPMentor.Core.WebhookFn.EvernoteAuth
    open FSharp.Control.Tasks.V2
    open System.Threading.Tasks

    let private audioTableKey = "AudioInfoTableKey"
    let private credTableKey = "CredTableKey"
    let private tableCache = ConcurrentDictionary<string, CloudTable>()

    let private buildAudioTable (ignored:string) = 
        let connStr = Environment.GetEnvironmentVariable("connStr")
        CloudStorageAccount.Parse(connStr)
                           .CreateCloudTableClient()
                           .GetTableReference("LPAudio")

    let private buildCredTable (ignored:string) = 
        let connStr = Environment.GetEnvironmentVariable("connStr")
        CloudStorageAccount.Parse(connStr)
                           .CreateCloudTableClient()
                           .GetTableReference("LPCredential")

    let getAudioInfoTable () =
        tableCache.GetOrAdd (audioTableKey, buildAudioTable)

    let getCredentialTable () =
        tableCache.GetOrAdd (audioTableKey, buildAudioTable)

    type AudioEntity (partition:string, rowId:string) =
        inherit TableEntity (partition, rowId)
        member val Text = "" with get, set
        member val Topic = "" with get, set
        member val Section = "" with get, set
        member val Order = Int32.MaxValue with get, set
        member val Lang = "en" with get, set
        member val BlobName = "" with get, set
    with
        static member ModifyWith (ni: NoteInfo, audioBlobName: string) (e: AudioEntity) =
            e.BlobName <- audioBlobName
            e.Text <- ni.Text
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

    type CredEntity (partition:string, rowId:string) =
        inherit TableEntity (partition, rowId)
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
end
