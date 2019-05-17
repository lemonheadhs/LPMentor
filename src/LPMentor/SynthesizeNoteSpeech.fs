module LPMentor.TTSFn.SynthesizeNoteSpeech

open System
open System.Threading.Tasks
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open Microsoft.Extensions.Logging
open FSharp.Azure.StorageTypeProvider.Table
open FSharp.Control.Tasks.V2

open LPMentor.Storage
open LPMentor.TTSFn.AzureSpeech


let parseQueueMsg (queueMsg: string) =
    ( lazy (String.IsNullOrEmpty queueMsg),
      lazy (queueMsg.Split("##") |> Array.toList))
    |> function
    | (Lazy true), _ -> None
    | _, Lazy (partition :: [rowKey]) -> Some (partition, rowKey)
    | _ -> None

let tryGetNoteData (partition, rowKey) = 
    let connStr = Environment.GetEnvironmentVariable("connStr")
    task {
        let! lpnoteEntity = lpnote.GetAsync (Row rowKey, Partition partition, connectionString= connStr) 
        return lpnoteEntity |> Option.map mapTo
    }

[<FunctionName("SynthesizeNoteSpeech")>]
let Run(
        [<QueueTrigger("notes", Connection = "connStr")>] 
        myQueueItem: string, 
        log: ILogger) =

    log.LogInformation(sprintf "C# Queue trigger function processed: %s" myQueueItem)
        
    let tnote =
        parseQueueMsg myQueueItem
        |> Option.map tryGetNoteData
        |> function
        | None -> task { return None }
        | Some m -> m
    
    task {
        match! tnote with
        | None -> ()
        | Some note ->
            let ssml = SSML.genDefaultSSML note.BaseInfo.Lang note.BaseInfo.Text
            let! resp =
                ssml
                |> sendTTS AudioOutputFormats.Audio_16k_128k_mono_mp3
            
            let audioFileName = 
                sprintf "%s/%i_%s.mp3" 
                        note.BaseInfo.Topic 
                        note.BaseInfo.Order 
                        note.BaseInfo.Section
            let container = audiosContainer.AsCloudBlobContainer()
            do! container.GetBlockBlobReference(audioFileName)
                         .UploadFromStreamAsync(resp.body)

            let audioInfo = note |> mapToAudioNote audioFileName
            let connStr = Environment.GetEnvironmentVariable("connStr")
            do! lpaudio.InsertAsync(
                    Partition audioInfo.Partition, 
                    Row audioInfo.RowId, 
                    audioInfo.BaseInfo, 
                    connectionString= connStr) |> Async.Ignore
    }

