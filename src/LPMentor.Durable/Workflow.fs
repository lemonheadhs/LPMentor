module LPMentor.Durable.Workflow

open System
open Microsoft.Azure.WebJobs
open Microsoft.WindowsAzure.Storage.Blob
open DurableFunctions.FSharp
open FSharp.Control.Tasks.V2
open System.Threading.Tasks
open System.IO

open LPMentor.Core.WebhookFn.Evernote
open LPMentor.Core.TTSFn.AzureSpeech
open LPMentor.Core.Models
open Storage.Blob
open Storage.Table
open System.Collections.Concurrent

let (<!+>) activityName f = Activity.define activityName f
let (<!->) activityName f = Activity.defineTask activityName f
let (<**>) activity p = Activity.call activity p

[<AutoOpen>]
module Activities = begin
    [<AutoOpen>]
    module ActivityFuncImpl = begin
        let internal credCache = ConcurrentDictionary<string, CredEntity>()
        let credExsists_ () =
            credCache.TryGetValue("cred") |> function
            | true, cred -> cred.ExpirationDate.AddHours(-3.) < DateTime.Now
            | false, _ -> false
            

        let fetchNote_ noteGuid =
            FetchNoteContent noteGuid
            |> Option.map (fun (content, metadata) ->
                                let noteInfo: NoteInfo = {
                                    Text = content
                                    Topic = metadata.Topic
                                    Section = metadata.Section
                                    Order = metadata.Order
                                    Lang = metadata.Lang
                                }
                                noteInfo)                

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

    let fetchCred = "FetchCred" <!+> fetchCred_
    let fetchNote = "FetchNote" <!+> fetchNote_
    let genAudio = "GenAudio" <!-> genAudio_
    let storeAudioInfo = "StoreAudioInfo" <!-> storeAudioInfo_
end


let workflow instanceId (webhookParam: WebhookParam) = orchestrator {
    let! credOpt = fetchCred <**> ()
    let mutable cred: CredEntity = CredEntity("", "")
    
    let! test =
        if credOpt |> Option.isSome then
            cred <- Option.get credOpt
            Orchestrator.ret ()
        else
            Orchestrator.ret ()
    let! optionNoteInfo = fetchNote <**> webhookParam.Guid_
    
    if Option.isSome optionNoteInfo then
        let noteInfo = Option.get optionNoteInfo
        let! audioFileName = 
            genAudio <**> noteInfo
        do! storeAudioInfo <**> struct(noteInfo, audioFileName)
        return ()
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