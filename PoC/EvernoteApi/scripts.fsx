#load "../../.paket/load/net472/PoC_Evernote/poc_evernote.group.fsx"

open System
open System.IO
open EvernoteSDK.Advanced
open Evernote.EDAM
open EvernoteSDK
open FSharp.Data

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let cwd = __SOURCE_DIRECTORY__


type Config = JsonProvider<"""{ "devToken": "token", "noteStoreUrl": "url", "consumerKey": "key", "consumerSecret": "secret" }""">
let config = Config.Load(Path.Combine [|cwd; "config.json"|])
let devTokenAuthConfig = (config.DevToken, config.NoteStoreUrl)
let oauthConfig = (config.ConsumerKey, config.ConsumerSecret, "sandbox.yinxiang.com")

// ENSession.SetSharedSessionDeveloperToken devTokenAuthConfig
ENSession.SetSharedSessionConsumerKey (config.ConsumerKey, config.ConsumerSecret, "app.yinxiang.com")
    
if not <| ENSession.SharedSession.IsAuthenticated then
    ENSession.SharedSession.AuthenticateToEvernote()


let myNotebookList = ENSession.SharedSession.ListNotebooks()
let myPlainNote = ENNote()

myPlainNote.Title <- "My plain text note"
myPlainNote.Content <- ENNoteContent.NoteContentWithString("Hello, world!")

let myPlainNoteRef = ENSession.SharedSession.UploadNote(myPlainNote, null)
let myFancyNote = ENNote()

myFancyNote.Title <- "My html note"
myFancyNote.Content <-
    """<p>Hello, world - <i>this</i> is a <b>fancy</b> note - and here is a table:</p>
       <br /> <br/><table border=\"1\" cellpadding=\"2\" cellspacing=\"0\" width=\"100%\"><tr><td>Red</td><td>Green</td></tr><tr><td>Blue</td><td>Yellow</td></tr></table>"""
    |> ENNoteContent.NoteContentWithSanitizedHTML

let myFancyNoteRef = ENSession.SharedSession.UploadNote(myFancyNote, null)
  // System.MissingMethodException: 找不到方法:“PreMailer.Net.InlineResult PreMailer.Net.PreMailer.MoveCssInline(System.String, Boolean, System.String)”

let textToFind = "world"
let myResultsList =
    ENSession.SharedSession.FindNotes
        (ENNoteSearch.NoteSearch(textToFind), null, ENSession.SearchScope.All,
         ENSession.SortOrder.RecentlyUpdated, 500)
let noteData = myResultsList.[0].NoteRef.AsData()

System.Text.Encoding.UTF8.GetString(noteData)
  // not what I wanted

open EvernoteSDK.Advanced

// ENSessionAdvanced.SetSharedSessionDeveloperToken devTokenAuthConfig

ENSessionAdvanced.SetSharedSessionConsumerKey (config.ConsumerKey, config.ConsumerSecret)

if not <| ENSessionAdvanced.SharedSession.IsAuthenticated then
  ENSessionAdvanced.SharedSession.AuthenticateToEvernote()

// --------- search ?

(*

let spec = NoteStore.NotesMetadataResultSpec()
spec.IncludeTitle <- true

let primaryStore = ENSessionAdvanced.SharedSession.PrimaryNoteStore


let searchNote (store: ENNoteStoreClient) spec searchTerms =
  let filter = NoteStore.NoteFilter()
  filter.Words <- searchTerms
  filter.Ascending <- false
  store.FindNotesMetadata(filter, 0, 100, spec)

let naiveSearch = searchNote primaryStore spec

naiveSearch "world" // """created:month-3"""

*)
// search syntax!!
// only oauth authenticated approach have permission to search
// and it seems that sandbox environment does no support search

// --------- get all text content ?
// get by guid id

let notebooks = ENSessionAdvanced.SharedSession.ListNotebooks()
notebooks.[0]

let noteStore =
  ENSessionAdvanced.SharedSession.PrimaryNoteStore

noteStore.ListNotebooks() |> Seq.map (fun nb -> nb.Name,nb.Guid)

noteStore

let inbox = noteStore.GetNotebook "3c9f6f3f-3ba8-4a2f-b236-a8588b91ea7b"

let note1 = 
  noteStore.GetNote ("e93720a3-91f0-4503-82e7-d125256a7cc5", 
                     withContent=true, 
                     withResourcesData=true, 
                     withResourcesRecognition=true, 
                     withResourcesAlternateData=true)
let note1Content =
  noteStore.GetNoteContent "e93720a3-91f0-4503-82e7-d125256a7cc5"

  // the content I get from API are in ENML mark up format, but I just want the pure inner text

// ----------- use FSharp.Data Xml TP to retrieve text

open FSharp.Data.HtmlDocument

let test =
  note1Content |> HtmlDocument.Parse

test.Descendants() 
|> Seq.head
|> HtmlNode.innerText

(fun _ ->
  use sw = System.IO.StreamWriter("content.txt")
  sw.Write (note1Content)
) ()

note1.TagNames

note1.Attributes

