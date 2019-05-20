module LPMentor.Core.WebhookFn.Evernote

open System
open System.Text.RegularExpressions
open Microsoft.AspNetCore.Http
open FSharp.Data

open EvernoteSDK.Advanced
open Evernote.EDAM
open EvernoteSDK


type EvernoteNotification =
    | NotebookCreate
    | NotebookUpdate
    | Create
    | Update
    | BusinessNotebookCreate
    | BusinessNotebookUpdate
    | BusinessCreate
    | BusinessUpdate
    with
    static member Parse (typeStr: string) =
        match typeStr with
        | "notebook_create" -> Some NotebookCreate
        | "notebook_update" -> Some NotebookUpdate
        | "create" -> Some Create
        | "update" -> Some Update
        | "business_notebook_create" -> Some BusinessNotebookCreate
        | "business_notebook_update" -> Some BusinessNotebookUpdate
        | "business_create" -> Some BusinessCreate
        | "business_update" -> Some BusinessUpdate
        | _ -> None

[<CLIMutable>]
type WebhookParam = {
    UserId: string
    NotebookGuid: string option
    Reason: EvernoteNotification
    Guid_: string
}

type Option<'T> with
    static member ap f opt =
        match f, opt with
        | Some fn, Some value -> Some (fn value)
        | _ -> None
let (<!>) = Option.map
let (<*>) = Option.ap

let parseParams (req: HttpRequest) =
    let queryDict = req.Query |> Seq.map (fun i -> i.Key, i.Value.ToString()) |> Map
    let queryVal key = Map.tryFind key queryDict
    let webhookParamCtr userId guid reason notebookGuid =
        { UserId = userId; Guid_ = guid; Reason = reason; NotebookGuid = notebookGuid }
    webhookParamCtr <!> (queryVal "userId") 
                    <*> (queryVal "guid") 
                    <*> (queryVal "reason" |> Option.bind EvernoteNotification.Parse)
                    <*> (Some (queryVal "notebookGuid")) 

type AudioNoteMetadata = {
    Topic: string
    Section: string
    Order: int
    Lang: string
}
let tryParseAudioNoteMetadata text =
    (* recognize the following text pattern and parse it:
        <lpmentor>
        Topic: *****
        Section: *****
        Order: 12
        Lang: en/cn
        </lpmentor>
    *)
    let extract (rgx: Regex) (groupName: string) text =
        rgx.IsMatch text |> function
        | false -> None
        | true ->
            let m = rgx.Match text
            m.Groups.[groupName].Value.Trim() |> Some
    let lpmentorRgx = 
        Regex("<lpmentor>(?<metadata>.*?)</lpmentor>", RegexOptions.Singleline)
    let topicRgx = Regex("Topic: (?<topic>.*)")
    let sectionRgx = Regex("Section: (?<section>.*)")
    let orderRgx = Regex("Order: (?<order>\d*)")
    let langRgx = Regex("Lang: (?<lang>.*)")
    let genMetadata topic section order lang =
        { Topic = topic; Section = section; Order = order; Lang = lang }
    text
    |> extract lpmentorRgx "metadata"
    |> Option.bind 
        (fun metadataString ->
            genMetadata <!> (extract topicRgx "topic" metadataString)
                        <*> (extract sectionRgx "section" metadataString)
                        <*> (extract orderRgx "order" metadataString |> Option.map Convert.ToInt32)
                        <*> (extract langRgx "lang" metadataString))

let innerTextWithLineBreak (n: HtmlNode) =
    let blockElems = 
        [| "p"; "h1"; "h2"; "h3"; "h4"; "h5"; "h6"; "ol"; "ul"; 
           "pre"; "address"; "blockquote"; "dl"; "div"; 
           "fieldset"; "form"; "hr"; "noscript"; "table"; "en-note" |]
        |> Set
    let exclusions = ["style" ; "script"]
    let rec innerTxt' (n': HtmlNode) =
        let name = n'.Name()
        ((lazy (String.IsNullOrEmpty name)), lazy (exclusions |> List.forall ((<>) name)))
        |> function
        | (Lazy true), _ -> n'.InnerText()
        | _, Lazy false -> ""
        | _ ->
            seq { 
                if blockElems |> Set.contains name then
                    let elems = n'.Elements()
                    for e in elems do
                        yield innerTxt' e
                    yield "\n"
                else
                    yield n'.InnerText() }
            |> String.Concat
    innerTxt' n

let markupInnerText markupString =
    let doc = HtmlDocument.Parse markupString
    doc.DescendantsWithPath (["en-note"])
    |> Seq.head
    |> fst
    |> innerTextWithLineBreak

let FetchNoteContent noteGuid =
    // ENSessionAdvanced.SetSharedSessionConsumerKey ("lemonhead-hs", "324f03fcfb2577bd")

    let devToken = Environment.GetEnvironmentVariable("devToken")
    let noteStoreUrl = Environment.GetEnvironmentVariable("noteStoreUrl")
    ENSessionAdvanced.SetSharedSessionDeveloperToken (devToken, noteStoreUrl)

    if not <| ENSessionAdvanced.SharedSession.IsAuthenticated then
        ENSessionAdvanced.SharedSession.AuthenticateToEvernote()

    let noteStore =
        ENSessionAdvanced.SharedSession.PrimaryNoteStore

    noteGuid
    |> noteStore.GetNoteTagNames 
    |> Seq.exists ((=) "LPMentor") |> function
    | false -> None
    | true ->
        let note1Content =
            noteGuid |> noteStore.GetNoteContent |> markupInnerText
        tryParseAudioNoteMetadata note1Content
        |> Option.map 
            (fun metadata ->
                note1Content, metadata)
