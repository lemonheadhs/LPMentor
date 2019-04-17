#load "../../.paket/load/net472/PoC_Evernote/poc_evernote.group.fsx"


open System
open System.IO
open EvernoteSDK.Advanced
open Evernote.EDAM.NoteStore
open EvernoteSDK

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let cwd = __SOURCE_DIRECTORY__

(*
NoteStore URL: 
https://sandbox.evernote.com/shard/s1/notestore
Expires:
   15 April 2020, 23:24
*)
let devToken = """"""

open EvernoteSDK
open Evernote.EDAM
open EvernoteOAuthNet

// let evernoteAuth = EvernoteOAuth

ENSession.SetSharedSessionDeveloperToken (devToken, "https://sandbox.evernote.com/shard/s1/notestore")

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
    ENSession.SharedSession.FindNotes(ENNoteSearch.NoteSearch(textToFind), null, ENSession.SearchScope.All, ENSession.SortOrder.RecentlyUpdated, 500)

let noteData = myResultsList.[0].NoteRef.AsData()

System.Text.Encoding.UTF8.GetString(noteData)
  // not what I wanted

open EvernoteSDK.Advanced

ENSessionAdvanced.SetSharedSessionDeveloperToken (devToken, "https://sandbox.evernote.com/shard/s1/notestore")

let filter = NoteStore.NoteFilter()
filter.Words <- "created:month-3"
// filter.NotebookGuid <- ""
// filter.TagGuids <- [|""|] |> ResizeArray
filter.Ascending <- false

let spec = NoteStore.NotesMetadataResultSpec()
spec.IncludeTitle <- true

let store = ENSessionAdvanced.SharedSession.PrimaryNoteStore
let ourNoteList = store.FindNotesMetadata(filter, 0, 100, spec)

ourNoteList.Notes
|> Seq.map (fun m -> store.GetNote(m.Guid, true, false, false, false).Content)
    // Content include xml tags


// search syntax!!





