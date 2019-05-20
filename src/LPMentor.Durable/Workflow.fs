module LPMentor.Core.Workflow

open Microsoft.Azure.WebJobs
open DurableFunctions.FSharp
open FSharp.Control.Tasks.V2
open System.Threading.Tasks

open LPMentor.Core.WebhookFn.Evernote
open LPMentor.Core.TTSFn.AzureSpeech
open LPMentor.Core.Models

let (<!+>) activityName f = Activity.define activityName f
let (<!->) activityName f = Activity.defineTask activityName f
let (<**>) activity p = Activity.call activity p

[<AutoOpen>]
module Activities = begin
    [<AutoOpen>]
    module ActivityFuncImpl = begin
        let fetchNote_ noteGuid =
            try
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
            with
            | _ -> None                        

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
                return (audioFileName, resp.body)
            }
    end

    let fetchNote = "fetchNote" <!+> fetchNote_
    let genAudio = "genAudio" <!-> genAudio_
end


let workflow (webhookParam: WebhookParam) = orchestrator {
    let! optionNoteInfo = fetchNote <**> webhookParam.Guid_
    
    // if false then
    //     ()

    //if Option.isSome optionNoteInfo then
    let noteInfo = Option.get optionNoteInfo
    let! pair = genAudio <**> noteInfo
    pair |> ignore
    //else
        //()
    // return [1];
    return 0
}

let test = orchestrator {
    let! t = fetchNote <**> ""

    if false then 
        t |> ignore
    else
        t |> ignore

    return 0
}

let test2 = task {
    let! one = task { return 1 }

    if false then ()
}
