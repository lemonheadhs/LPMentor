module LPMentor.Durable.Plain

open System
open System.Threading.Tasks
open Microsoft.Azure.WebJobs
open FSharp.Control.Tasks.V2

open LPMentor.Core.Activities
open LPMentor.Core.WebhookFn.Evernote
open LPMentor.Core.Models

[<FunctionName("NoteIngest")>]
let run ([<OrchestrationTrigger>] context: DurableOrchestrationContext) = task {
    let webhookParam = context.GetInput<WebhookParam>()
    let! firstTry = context.CallActivityAsync<Result<NoteInfo, FetchNoteError>>("fetchNote_", webhookParam.Guid_) 
    let mutable rNoteInfo: Result<NoteInfo, FetchNoteError> = Error NoteNotFound
    if firstTry |> isCredExpiredError then
        let maxWaitDuration = TimeSpan.FromDays 7.
        let! s = context.WaitForExternalEvent<string>("RenewAuth", maxWaitDuration)
        let! noteContent = context.CallActivityAsync<Result<NoteInfo, FetchNoteError>>("fetchNote_",  webhookParam.Guid_)
        rNoteInfo <- noteContent
    else
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
            |> List.map (fun p -> context.CallActivityAsync<string>("genAudioFile_", p))
            |> Task.WhenAll
        do! context.CallActivityAsync("mergeFiles_", struct(List.ofArray(files), mergedFileName))
        do! context.CallActivityAsync("storeAudioInfo_", struct(noteInfo, mergedFileName))
        do! context.CallActivityAsync("updateCatalog_", noteInfo.Topic)
    return ()
}

[<FunctionName("fetchNote_")>]
let FetchNote([<ActivityTrigger>] noteGuid) = fetchNote_ noteGuid

[<FunctionName("genAudioFile_")>]
let GenAudioFile([<ActivityTrigger>] p) = genAudioFile_ p

[<FunctionName("mergeFiles_")>]
let MergeFiles([<ActivityTrigger>] p) = mergeFiles_ p

[<FunctionName("storeAudioInfo_")>]
let StoreAudioInfo([<ActivityTrigger>] p) = storeAudioInfo_ p

[<FunctionName("updateCatalog_")>]
let UpdateCatalog([<ActivityTrigger>] p) = updateCatalog_ p
