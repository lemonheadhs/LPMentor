#load "../../.paket/load/net472/PoC_AzureSpeech/Hopac.fsx"
#load "../../.paket/load/net472/PoC_AzureSpeech/poc_azurespeech.group.fsx"

open System
open System.IO
open System.Threading.Tasks
// open Microsoft.CognitiveServices.Speech
open FSharp.Control.Tasks.V2
open FSharp.Data
open HttpFs.Client
open HttpFs.Composition
open Hopac

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let cwd = __SOURCE_DIRECTORY__

let toStr obj = obj.ToString()

type Config = JsonProvider<"""{ "key": "abc", "region": "abc" }""", ResolutionFolder = __SOURCE_DIRECTORY__>
let configJson = Path.Combine [|cwd; "config.json"|] |> Config.Load

// ------

let issueTokenEndpoint = "https://eastasia.api.cognitive.microsoft.com/sts/v1.0/issueToken"
let OpcApimSubscriptionKeyHeader key = Custom ("Ocp-Apim-Subscription-Key", key) |> Request.setHeader
let BearerAuthHeader = sprintf "Bearer %s" >> Authorization >> Request.setHeader
let tokenValidPeriod = 10. - 1. // in minutes

type Token = {
    Body: string
    ExpiresAt: DateTime
}

let getAuthToken () =
    let req =
        Request.createUrl Post issueTokenEndpoint
        |> OpcApimSubscriptionKeyHeader configJson.Key
        |> Request.setHeader (ContentType (ContentType.create ("application", "x-www-form-urlencoded")))
    job {
        let! resp = getResponse req
        let! tokenContent = Response.readBodyAsString resp
        return {
            Body = tokenContent
            ExpiresAt = DateTime.Now.AddMinutes(tokenValidPeriod)
        }
    }

let mutable token = getAuthToken () |> run

let getRefreshedToken () =
    if DateTime.Now > token.ExpiresAt then
        token <- getAuthToken () |> run
    token.Body

// ------

let getVoicesEndpoint = "https://eastasia.tts.speech.microsoft.com/cognitiveservices/voices/list"

let getVoiceList () =
    let req =
        Request.createUrl Get getVoicesEndpoint
        |> BearerAuthHeader (getRefreshedToken())
    job {
        let! resp = getResponse req
        return resp
    } |> run

getVoiceList ()
|> Response.readBodyAsString |> run

// ------

module AudioOutputFormats =
    let Raw_16k_16_mono_pcm = "raw-16khz-16bit-mono-pcm"
    let Raw_8k_8_mono_mulaw = "raw-8khz-8bit-mono-mulaw"
    let Riff_8k_8_mono_alaw = "riff-8khz-8bit-mono-alaw"
    let Riff_8k_8_mono_mulaw = "riff-8khz-8bit-mono-mulaw"
    let Riff_16k_16_mono_pcm = "riff-16khz-16bit-mono-pcm"
    let Audio_16k_128k_mono_mp3 = "audio-16khz-128kbitrate-mono-mp3"
    let Audio_16k_64k_mono_mp3 = "audio-16khz-64kbitrate-mono-mp3"
    let Audio_16k_32k_mono_mp3 = "audio-16khz-32kbitrate-mono-mp3"
    let Raw_24k_16_monp_pcm = "raw-24khz-16bit-mono-pcm"
    let Riff_24k_16_monp_pcm = "riff-24khz-16bit-mono-pcm"
    let Audio_24k_160k_mono_mp3 = "audio-24khz-160kbitrate-mono-mp3"
    let Audio_24k_96k_mono_mp3 = "audio-24khz-96kbitrate-mono-mp3"
    let Audio_24k_48k_mono_mp3 ="audio-24khz-48kbitrate-mono-mp3"


module Test =
    let fmt = AudioOutputFormats.Audio_16k_128k_mono_mp3

    let text2speechEndpoint = 
        "https://eastasia.tts.speech.microsoft.com/cognitiveservices/v1"


    let textStr = """<speak version='1.0' xml:lang='en-US'><voice xml:lang='en-US' xml:gender='Female'
        name='Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)'>
            Microsoft Speech Service Text-to-Speech API
    </voice></speak>"""

    let testTTS fmt =
        let req =
            Request.createUrl Post text2speechEndpoint
            |> Request.setHeader (Custom ("X-Microsoft-OutputFormat", fmt))
            |> Request.setHeader (ContentType.create ("application", "ssml+xml") |> ContentType)
            |> BearerAuthHeader (getRefreshedToken())
            |> Request.setHeader (Custom ("Host", "eastasia.tts.speech.microsoft.com"))
            |> Request.setHeader (UserAgent "Fsharp Interactive")
            |> Request.body (BodyString textStr)
        job {
            let! resp = getResponse req
            return resp
        } |> run

    let test = testTTS AudioOutputFormats.Audio_16k_128k_mono_mp3

    let saveAudioFile (fmt, fileName) =
        let resp = testTTS fmt
        use file = File.Create fileName
        resp.body.CopyToAsync(file).Wait()

    (AudioOutputFormats.Audio_16k_128k_mono_mp3, "rest.mp3")
    |> saveAudioFile

