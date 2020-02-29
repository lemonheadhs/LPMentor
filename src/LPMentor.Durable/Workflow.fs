module LPMentor.Durable.Workflow

open System
open Microsoft.Azure.WebJobs
open DurableFunctions.FSharp
open System.Threading.Tasks
open FSharp.Core

open LPMentor.Core.WebhookFn.Evernote
open LPMentor.Core.Models
open FSharp.Control.Tasks
open LPMentor.Core.Activities

let (<!+>) activityName f = Activity.define activityName f
let (<!->) activityName f = Activity.defineTask activityName f
let (<**>) activity p = Activity.call activity p


let fetchNote = "FetchNote" <!-> fetchNote_
let genAudioFile = "GenAudioFile" <!-> genAudioFile_
let mergeFiles = "MergeFiles" <!-> mergeFiles_
let storeAudioInfo = "StoreAudioInfo" <!-> storeAudioInfo_

[<Obsolete>]
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


// [<FunctionName("FetchNote")>]
// let FetchNote([<ActivityTrigger>] noteGuid) = fetchNote.run noteGuid

// [<FunctionName("GenAudioFile")>]
// let GenAudioFile([<ActivityTrigger>] p) = genAudioFile.run p

// [<FunctionName("MergeFiles")>]
// let MergeFiles([<ActivityTrigger>] p) = mergeFiles.run p

// [<FunctionName("StoreAudioInfo")>]
// let StoreAudioInfo([<ActivityTrigger>] p) = storeAudioInfo.run p

// [<FunctionName("NoteIngest")>]
// let Run([<OrchestrationTrigger>] context: DurableOrchestrationContext) =
//     Orchestrator.run (workflow context.InstanceId, context)