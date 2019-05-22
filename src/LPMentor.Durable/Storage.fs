module LPMentor.Durable.Storage

open System
open Microsoft.Azure.Storage
open System.Collections.Concurrent
open System.Collections.Specialized.BitVector32

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
    open System.Threading.Tasks
    open FSharp.Control.Tasks.V2

    let private tableKey = "AudioInfoTableKey"
    let private tableCache = ConcurrentDictionary<string, CloudTable>()

    let private buildTable (ignored:string) = 
        let connStr = Environment.GetEnvironmentVariable("connStr")
        CloudStorageAccount.Parse(connStr)
                           .CreateCloudTableClient()
                           .GetTableReference("LPAudio")

    let getAudioInfoTable () =
        tableCache.GetOrAdd (tableKey, buildTable)

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
end
