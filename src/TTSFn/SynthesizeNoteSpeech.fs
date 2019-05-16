namespace TTSFn

open System
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host
open Microsoft.Extensions.Logging

module SynthesizeNoteSpeech =

    [<FunctionName("SynthesizeNoteSpeech")>]
    let Run(
            [<QueueTrigger("notes", Connection = "connStr")>] 
            myQueueItem: string, 
            log: ILogger) =
    
        log.LogInformation(sprintf "C# Queue trigger function processed: %s" myQueueItem)
    


