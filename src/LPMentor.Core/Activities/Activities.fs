module LPMentor.Core.Activities

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Evernote.EDAM.NoteStore
open System.Text
open System.IO

open Storage.Blob
open Storage.Table
open LPMentor.Core.WebhookFn.Evernote
open LPMentor.Core.Models
open LPMentor.Core.TTSFn.AzureSpeech


let inline (>>=) (m: Task<'a>) (f: 'a -> Task<'b>) =
    task {
        let! a = m
        return! (f a)
    }

let internal credCache = ConcurrentDictionary<string, CredEntity>()
let ofValidCred (cred: CredEntity) = 
    cred.ExpirationDate.AddHours(-3.) > DateTime.Now |> function
    | true -> Some cred
    | false -> None
let getValidCachedCred () =
    credCache.TryGetValue("cred") |> function
    | true, cred -> ofValidCred cred
    | _ -> None
let internal noteStoreCache = ConcurrentDictionary<string, NoteStore.Client>()
let getCachedNoteStore (cred: CredEntity) =
    noteStoreCache.GetOrAdd(cred.NoteStoreUrl, noteStoreClient)

type FetchNoteError = | CredExpired | NoteNotFound

let fetchNote_ noteGuid =
    task {
        let! credOpt =
            getValidCachedCred() |> function
            | Some cred -> Task.FromResult (Some cred)
            | None -> 
                CredEntity.TryGet() 
                >>= (   Option.bind ofValidCred
                     >> Option.map (fun validCred ->
                                        credCache.AddOrUpdate("cred", validCred, (fun s c -> validCred)))
                     >> Task.FromResult)
        let contentResult =
            credOpt
            |> Result.ofOption CredExpired
            |> Result.bind (
                fun cred ->
                    let noteStore = getCachedNoteStore cred
                    FetchNoteContentWith noteStore cred.AuthenticationToken noteGuid
                    |> Result.ofOption NoteNotFound)
            |> Result.map (
                fun (content, metadata) ->
                    let noteInfo: NoteInfo = {
                        Text = content
                        Topic = metadata.Topic
                        Section = metadata.Section
                        Order = metadata.Order
                        Lang = metadata.Lang
                    }
                    noteInfo)  
        return contentResult
    }        

let splitTextSize size (s:string) =
    let originLength = s.Length
    let mutable pointer = 0
    let sb = StringBuilder()
    seq {
        while originLength - pointer > 0 do
            let mutable span = Math.Min (originLength - pointer, size)
            sb.Append(s, pointer, span) |> ignore
            pointer <- pointer + span
            span <- 0
            while pointer + span < originLength && Char.IsLetterOrDigit(s, pointer + span) do            
                span <- span + 1
            if span > 0 then
                sb.Append(s, pointer, span) |> ignore
                pointer <- pointer + span
                span <- 0
            yield sb.ToString()
            sb.Length <- 0
    }

let splitText = splitTextSize 6000

let genAudioFile_ struct(fileName:string, lang, textContent) =
    let ssml = SSML.genDefaultSSML lang textContent
    task {
        let! resp = 
            ssml
            |> sendTTS AudioOutputFormats.Audio_16k_128k_mono_mp3
        let container = getAudioContainer ()
        do! container.GetBlockBlobReference(fileName)
                     .UploadFromStreamAsync(resp.body)
        return fileName
    }

let mergeInMemThen (fn:string -> Stream -> Task) 
                   struct(files:string list, mergedFileName:string) =
    let container = getAudioContainer ()
    task {
        let ms = new MemoryStream()
        for file in files do
            do! (container.GetBlockBlobReference(file)
                         .DownloadToStreamAsync (ms))
        ms.Position <- 0L
        do! (fn mergedFileName ms)
        ms.Dispose()
    }
let pushFile mergedFileName stream =
    let container = getAudioContainer ()
    container.GetBlockBlobReference(mergedFileName)
                .UploadFromStreamAsync(stream)
let mergeFiles_ = mergeInMemThen pushFile

let storeAudioInfo_ struct(ni: NoteInfo, audioFileName: string) =
    // why ValueTuple? ActivityFunction does not accept multiple parameters
    //  https://github.com/Azure/azure-functions-durable-extension/issues/152
    task {
        let! result = AudioEntity.Save (ni, audioFileName)
        return ()
    }

let updateCatalog_ (topic: string) =
    task {
        let! result =
            CatalogEntity.Recollect topic
            |> CatalogEntity.Save
        return ()
    }

let isCredExpiredError (r: Result<NoteInfo, FetchNoteError>) =
    match r with
    | Error CredExpired -> true
    | _ -> false
