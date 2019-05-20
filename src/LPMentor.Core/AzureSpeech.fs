module LPMentor.Core.TTSFn.AzureSpeech

open System
open HttpFs.Client
open Hopac
open FSharp.Data
open System.Collections.Concurrent


module Auth = begin
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
            |> OpcApimSubscriptionKeyHeader (Environment.GetEnvironmentVariable("subscriptionKey"))
            |> Request.setHeader (ContentType (ContentType.create ("application", "x-www-form-urlencoded")))
        job {
            let! resp = getResponse req
            let! tokenContent = Response.readBodyAsString resp
            return {
                Body = tokenContent
                ExpiresAt = DateTime.Now.AddMinutes(tokenValidPeriod)
            }
        }

    let private tkey = "token_store_key"
    let private tokenCache = ConcurrentDictionary<string, Token>()

    let getRefreshedToken () =
        let fetch = getAuthToken >> run
        let token =
            tokenCache.GetOrAdd(tkey, ignore >> fetch) |> function
            | t when DateTime.Now > t.ExpiresAt ->
                let t' = fetch()
                tokenCache.[tkey] <- t'
                t'
            | t -> t
        token.Body

end

module SSML = begin
    open System.Xml.Linq

    [<Literal>]
    let ssmlSample = """
<speak version='1.0' xml:lang='en-US'>
    <voice xml:lang='en-US' xml:gender='Female'
    name='Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)'>
        Microsoft Speech Service Text-to-Speech API
    </voice>
</speak>"""

    type SSML = XmlProvider<ssmlSample>

    let xn = XName.Get
    let xnn s = XName.Get (s, "http://www.w3.org/XML/1998/namespace")

    [<AutoOpen>]
    module Voice = begin
        type Voice = { Name: string ; Gender: string ; Lang: string }

        let Zira = { Lang = "en-US"; Gender = "Female"; Name = "en-US-ZiraRUS" }
        let Jessa = { Lang = "en-US"; Gender = "Female"; Name = "en-US-JessaRUS" }
        let Benjamin = { Lang = "en-US"; Gender = "Male"; Name = "en-US-BenjaminRUS" }
        let Jessa24k = { Lang = "en-US"; Gender = "Female"; Name = "en-US-Jessa24kRUS" }
        let Guy24k = { Lang = "en-US"; Gender = "Male"; Name = "en-US-Guy24kRUS" }
        let Huihui = { Lang = "zh-CN"; Gender = "Female"; Name = "zh-CN-HuihuiRUS" }
        let Yaoyao = { Lang = "zh-CN"; Gender = "Female"; Name = "zh-CN-Yaoyao-Apollo" }
        let Kangkang = { Lang = "zh-CN"; Gender = "Male"; Name = "zh-CN-Kangkang-Apollo" }    

        type Lang = | English | Chinese
            with 
            static member defaultVoice = function
                | English -> Zira
                | Chinese -> Huihui
            static member parse = function
                | "en" -> English
                | "cn" -> Chinese
                | _ -> failwith "invalid language option"
    end

    let genSSML voice content =
        let xvoice = SSML.Voice (voice.Lang, voice.Gender, voice.Name, content)
        let speak =
            SSML.Speak (XElement (xn "speak", 
                                  XAttribute(xn "version", "1.0"), 
                                  XAttribute(xnn "lang", voice.Lang), 
                                  xvoice.XElement))
        speak.ToString()

    let genDefaultSSML lang content =    
        let voice = lang |> Lang.parse |> Lang.defaultVoice
        genSSML voice content
end

module AudioOutputFormats = begin
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
end

let t2sEndpoint = 
    "https://eastasia.tts.speech.microsoft.com/cognitiveservices/v1"

let sendTTS fmt ssmlStr =
    let req =
        Request.createUrl Post t2sEndpoint
        |> Request.setHeader (Custom ("X-Microsoft-OutputFormat", fmt))
        |> Request.setHeader (ContentType.create ("application", "ssml+xml") |> ContentType)
        |> Auth.BearerAuthHeader (Auth.getRefreshedToken())
        |> Request.setHeader (Custom ("Host", "eastasia.tts.speech.microsoft.com"))
        |> Request.setHeader (UserAgent "AzFunc LPMentor App")
        |> Request.body (BodyString ssmlStr)
    job {
        let! resp = getResponse req
        return resp
    } |> Job.toAsync




