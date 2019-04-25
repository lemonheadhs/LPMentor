#load "../../.paket/load/net472/PoC_AzureSpeech/poc_azurespeech.group.fsx"

open System
open System.IO
open System.Threading.Tasks
open Microsoft.CognitiveServices.Speech
open FSharp.Control.Tasks.V2
open FSharp.Data

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let cwd = __SOURCE_DIRECTORY__

let toStr obj = obj.ToString()

type Config = JsonProvider<"""{ "key": "abc", "region": "abc" }""", ResolutionFolder = __SOURCE_DIRECTORY__>
let configJson = Path.Combine [|cwd; "config.json"|] |> Config.Load

let SynthesisToSpeakerAsync text =

    // Creates an instance of a speech config with specified subscription key and service region.
    // Replace with your own subscription key and service region (e.g., "westus").
    // The default language is "en-us".
    let config = SpeechConfig.FromSubscription(configJson.Key, configJson.Region)

    // Creates a speech synthesizer using speaker as audio output.
    use synthesizer = new SpeechSynthesizer(config)
   
    task {

        use! result = synthesizer.SpeakTextAsync(text)

        match result.Reason with
        | ResultReason.SynthesizingAudioCompleted ->
            Console.WriteLine (sprintf "Speech synthesized to speaker for text [%s]" text)
        
        | ResultReason.Canceled ->
            let cancellation = SpeechSynthesisCancellationDetails.FromResult(result)
            Console.WriteLine (sprintf "CANCELED: Reason=%s" (cancellation.Reason |> toStr))

            if (cancellation.Reason = CancellationReason.Error) then
            
                Console.WriteLine(sprintf "CANCELED: ErrorCode=%s" (cancellation.ErrorCode |> toStr))
                Console.WriteLine(sprintf "CANCELED: ErrorDetails=[%s]" cancellation.ErrorDetails)
                Console.WriteLine("CANCELED: Did you update the subscription info?")
        
        | _ -> ()

        // This is to give some time for the speaker to finish playing back the audio
        do! Task.Delay 500
        Console.WriteLine("Done ...")
    }
            
"hello, lemonhead" |> SynthesisToSpeakerAsync |> Task.WaitAll
    

    
        





