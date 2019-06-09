module LPMentor.Durable.Workflow

open System
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

        let genAudio_ (noteInfo: NoteInfo) =
            let ssml = SSML.genDefaultSSML noteInfo.Lang noteInfo.Text
            task {
                let! resp = 
                    ssml
                    |> sendTTS AudioOutputFormats.Audio_16k_128k_mono_mp3
                let audioFileName = 
                    sprintf "%s/%i_%s.mp3" 
                            noteInfo.Topic 
                            noteInfo.Order 
                            noteInfo.Section
                let container = getAudioContainer ()
                do! container.GetBlockBlobReference(audioFileName)
                             .UploadFromStreamAsync(resp.body)
                return audioFileName
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
    let genAudio = "GenAudio" <!-> genAudio_
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
        let! audioFileName = 
            genAudio <**> noteInfo
        do! storeAudioInfo <**> struct(noteInfo, audioFileName)
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


[<FunctionName("FetchNote")>]
let FetchNote([<ActivityTrigger>] noteGuid) = fetchNote.run noteGuid

[<FunctionName("GenAudio")>]
let GenAudio([<ActivityTrigger>] noteInfo) = genAudio.run noteInfo

[<FunctionName("StoreAudioInfo")>]
let StoreAudioInfo([<ActivityTrigger>] p) = storeAudioInfo.run p

[<FunctionName("NoteIngest")>]
let Run([<OrchestrationTrigger>] context: DurableOrchestrationContext) =
    Orchestrator.run (workflow context.InstanceId, context)