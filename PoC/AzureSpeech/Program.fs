// Learn more about F# at http://fsharp.org

open System
open System.IO
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open FSharp.Data
open Microsoft.CognitiveServices.Speech

type Config = JsonProvider<"""{ "key": "abc", "region": "abc" }""", ResolutionFolder = __SOURCE_DIRECTORY__>
let configJson = Path.Combine [|"config.json"|] |> Config.Load

let toStr obj = obj.ToString()

let SynthesisToSpeakerAsync text =

    // Creates an instance of a speech config with specified subscription key and service region.
    // Replace with your own subscription key and service region (e.g., "westus").
    // The default language is "en-us".
    let config = SpeechConfig.FromSubscription(configJson.Key, configJson.Region)

    task {
   
        // Creates a speech synthesizer using speaker as audio output.
        use synthesizer = new SpeechSynthesizer(config)

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
        do! Task.Delay 5000
        Console.WriteLine("Done ...")
    }

let prompt words =
    sprintf "%s: " words |> Console.WriteLine
    let input = Console.ReadLine ()
    if String.IsNullOrEmpty input then
        None
    else
        Some input

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    let rec test () =
        match prompt "type in text to synthesize speech" with
        | Some text ->
            (SynthesisToSpeakerAsync text).Wait()
            test ()
        | None -> ()
    test ()        
    0 // return an integer exit code
