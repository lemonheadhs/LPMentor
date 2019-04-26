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

let text2speechEndpoint = 
    "https://eastasia.tts.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1"


let textStr = """<speak version='1.0' xml:lang='en-US'><voice xml:lang='en-US' xml:gender='Female'
    name='Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)'>
        Microsoft Speech Service Text-to-Speech API
</voice></speak>"""

let testTTS () =
    let req =
        Request.createUrl Post text2speechEndpoint
        |> Request.setHeader (Custom ("X-Microsoft-OutputFormat", "raw-16khz-16bit-mono-pcm"))
        |> Request.setHeader (ContentType.create ("application", "ssml+xml") |> ContentType)
        |> BearerAuthHeader (getRefreshedToken())
        |> Request.setHeader (Custom ("Host", "eastasia.tts.speech.microsoft.com"))
        |> Request.setHeader (UserAgent "Fsharp Interactive")
        |> Request.body (BodyString textStr)
    job {
        let! resp = getResponse req
        return resp
    } |> run

testTTS()
  // 400 Bad Request ?
