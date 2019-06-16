module LPMentor.Durable.Workflow

open System
open System.IO
open System.Text
open Microsoft.Azure.WebJobs
open DurableFunctions.FSharp
open FSharp.Control.Tasks.V2
open System.Threading.Tasks
open FSharp.Core

open LPMentor.Core.WebhookFn.Evernote
open LPMentor.Core.WebhookFn.EvernoteAuth
open LPMentor.Core.TTSFn.AzureSpeech
open LPMentor.Core.Models
open Storage.Blob
open Storage.Table
open System.Collections.Concurrent
open Evernote.EDAM.NoteStore
open FSharp.Control.Tasks
open System.Diagnostics

let (<!+>) activityName f = Activity.define activityName f
let (<!->) activityName f = Activity.defineTask activityName f
let (<**>) activity p = Activity.call activity p

let inline (>>=) (m: Task<'a>) (f: 'a -> Task<'b>) =
    task {
        let! a = m
        return! (f a)
    }

[<AutoOpen>]
module Activities = begin
    [<AutoOpen>]
    module ActivityFuncImpl = begin
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

        let splitText (s:string) = 
            let size = 8000
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

        let mergeFiles_ struct(files:string list, mergedFileName:string) =
            let container = getAudioContainer ()
            task {
                let ms = MemoryStream()
                for file in files do
                    do! (container.GetBlockBlobReference(file)
                                 .DownloadToStreamAsync (ms))
                ms.Position <- 0L
                do! container.GetBlockBlobReference(mergedFileName)
                            .UploadFromStreamAsync(ms)
                ms.Dispose()
            }

        let storeAudioInfo_ struct(ni: NoteInfo, audioFileName: string) =
            // why ValueTuple? ActivityFunction does not accept multiple parameters
            //  https://github.com/Azure/azure-functions-durable-extension/issues/152
            task {
                let! result = AudioEntity.Save (ni, audioFileName)
                return ()
            }
    end

    let fetchNote = "FetchNote" <!-> fetchNote_
    let genAudioFile = "GenAudioFile" <!-> genAudioFile_
    let mergeFiles = "MergeFiles" <!-> mergeFiles_
    let storeAudioInfo = "StoreAudioInfo" <!-> storeAudioInfo_
end

let isCredExpiredError (r: Result<NoteInfo, FetchNoteError>) =
    match r with
    | Error CredExpired -> true
    | _ -> false

let workflow instanceId (webhookParam: WebhookParam) = orchestrator {
    
    let! firstTry = fetchNote <**> webhookParam.Guid_
    let mutable rNoteInfo: Result<NoteInfo, FetchNoteError> = Error NoteNotFound
    // if firstTry |> isCredExpiredError then
    //     let maxWaitDuration = TimeSpan.FromDays 7.
    //     let! s = Orchestrator.waitForEvent<string> maxWaitDuration "RenewAuth"
    //     let! noteContent = fetchNote <**> webhookParam.Guid_
    //     rNoteInfo <- noteContent
    // else
    //     rNoteInfo <- firstTry
    // DurableFunction.FSharp now has this bug, you can not use 2 if-else block sequentially in one orchestrator workflow..

    rNoteInfo <- firstTry

    let optionNoteInfo = rNoteInfo |> function
                         | Ok i -> Some i
                         | _ -> None
    if Option.isSome optionNoteInfo then
        let noteInfo = Option.get optionNoteInfo
        let segments = splitText noteInfo.Text |> Seq.toList
        let segmentNames = 
            [1..segments.Length]
            |> List.map (sprintf "%s/portions/%i_%s_%i.mp3"
                                noteInfo.Topic
                                noteInfo.Order
                                noteInfo.Section)
        let mergedFileName = 
            sprintf "%s/%i_%s.mp3" 
                    noteInfo.Topic 
                    noteInfo.Order 
                    noteInfo.Section
        let! files =
            segments
            |> List.map2 (fun name content -> struct(name, noteInfo.Lang, content)) segmentNames
            |> List.map (Activity.call genAudioFile)
            |> Activity.all
        do! mergeFiles <**> struct(files, mergedFileName)
        do! storeAudioInfo <**> struct(noteInfo, mergedFileName)
        return ()
}

let testwf = orchestrator {
    let! i = Orchestrator.ret 123
    if true then
        do! Orchestrator.ret ()
        if true then
            do! Orchestrator.ret ()
        else
            do! Orchestrator.ret ()
    else
        do! Orchestrator.ret ()
    // let i2 = 12
    // let! i3 = Orchestrator.ret 45

    //  # the following if block will lead to error!
    // if true then
    //     do! Orchestrator.ret ()
    // return ()
}

let testTaskWf = task {
    let! i = Task.FromResult 12
    if true then
        do! Task.FromResult ()
    else
        do! Task.FromResult ()
    
    // task workflow does not have the same issue as orchestrator workflow
    if true then
        ()
}

let hardWork = 
    fun item -> task {
      do! Task.Delay 1000
      return sprintf "Worked hard on %s!" item
    }
    |> Activity.defineTask "HardWork"

[<FunctionName("HardWork")>]
let HardWork([<ActivityTrigger>] name) = hardWork.run name


let testwf2 instanceId (webhookParam: WebhookParam) = orchestrator {
    do! Orchestrator.delay (TimeSpan.FromSeconds 5.)
    let! files =
        ["1"; "2"]
        |> List.map (Activity.call hardWork)
        |> Activity.all
    
    do! Orchestrator.delay (TimeSpan.FromSeconds 5.)
    // Function 'NoteIngest (Orchestrator)' failed with an error. 
    // Reason: System.InvalidOperationException: Multithreaded execution was detected
    // https://microsoft.com/en-us/azure/azure-functions/durable-functions-checkpointing-and-replay#orchestrator-code-constraints.
    return ()
}


[<FunctionName("FetchNote")>]
let FetchNote([<ActivityTrigger>] noteGuid) = fetchNote.run noteGuid

[<FunctionName("GenAudioFile")>]
let GenAudioFile([<ActivityTrigger>] p) = genAudioFile.run p

[<FunctionName("MergeFiles")>]
let MergeFiles([<ActivityTrigger>] p) = mergeFiles.run p

[<FunctionName("StoreAudioInfo")>]
let StoreAudioInfo([<ActivityTrigger>] p) = storeAudioInfo.run p

[<FunctionName("NoteIngest")>]
let Run([<OrchestrationTrigger>] context: DurableOrchestrationContext) =
    Orchestrator.run (workflow context.InstanceId, context)