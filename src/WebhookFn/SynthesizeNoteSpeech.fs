module LPMentor.TTSFn.SynthesizeNoteSpeech

open System
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open Microsoft.Extensions.Logging
open FSharp.Azure.StorageTypeProvider.Table

open LPMentor.Storage


let parseQueueMsg (queueMsg: string) =
    (lazy (String.IsNullOrEmpty queueMsg),
     lazy (queueMsg.Split("##") |> Array.toList))
    |> function
    | (Lazy true), _ -> None
    | _, Lazy (partition :: [rowKey]) -> Some (partition, rowKey)
    | _ -> None

let tryGetNoteData (partition, rowKey) = 
    lpnote
    
    ()

[<FunctionName("SynthesizeNoteSpeech")>]
let Run(
        [<QueueTrigger("notes", Connection = "connStr")>] 
        myQueueItem: string, 
        log: ILogger) =

    log.LogInformation(sprintf "C# Queue trigger function processed: %s" myQueueItem)
        
    


