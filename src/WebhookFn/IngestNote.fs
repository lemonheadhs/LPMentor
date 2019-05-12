namespace WebhookFn

open System
open System.IO
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Newtonsoft.Json
open FSharp.Data
open FSharp.Azure.StorageTypeProvider
open FSharp.Azure.StorageTypeProvider.Queue

open EvernoteSDK.Advanced
open Evernote.EDAM
open EvernoteSDK

module IngestNote =
    [<CLIMutable>]
    type NoteInfo = {
        Text: string
        Topic: string
        Section: string
        Order: int
    }
    type NoteInfoEx = {
        Partition: string
        RowId: string
        BaseInfo: NoteInfo
    }
    [<CLIMutable>]
    type AudioNoteInfo = {
        Text: string
        Topic: string
        Section: string
        Order: int
        BlobUrl: string
    }
    type AudioNoteInfoEx = {
        Partition: string
        RowId: string
        BaseInfo: AudioNoteInfo
    }
    let FetchNoteContent noteGuid =

        // ENSessionAdvanced.SetSharedSessionConsumerKey ("lemonhead-hs", "324f03fcfb2577bd")

        let devToken = Environment.GetEnvironmentVariable("devToken")
        let noteStoreUrl = Environment.GetEnvironmentVariable("noteStoreUrl")
        ENSessionAdvanced.SetSharedSessionDeveloperToken (devToken, noteStoreUrl)

        if not <| ENSessionAdvanced.SharedSession.IsAuthenticated then
            ENSessionAdvanced.SharedSession.AuthenticateToEvernote()

        let noteStore =
            ENSessionAdvanced.SharedSession.PrimaryNoteStore

        let note1Content =
            noteStore.GetNoteContent noteGuid
            // null check?

        note1Content

    let markupInnerText markupString =
        let doc = HtmlDocument.Parse markupString
        doc.Descendants()
        |> Seq.head
        |> HtmlNode.innerText

    type Azure = AzureTypeProvider<"UseDevelopmentStorage=true", autoRefresh=5>
    let noteQueue = Azure.Queues.notes

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

    [<FunctionName "IngestNote">]
    let Run(
            [<HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*any}")>] req: HttpRequest,
            log: ILogger) =
    
        log.LogInformation("F# HTTP trigger function processed a request.")

        match parseParams req with
        | None -> task { return (BadRequestObjectResult ("Please pass a name on the query string or in the request body") :> IActionResult) }
        | Some webhookParams ->

            let note1Content = 
                FetchNoteContent webhookParams.Guid_ |> markupInnerText
                // FetchNoteContent "e93720a3-91f0-4503-82e7-d125256a7cc5" |> markupInnerText

            task {
                do! noteQueue.Enqueue (note1Content, connectionString= Environment.GetEnvironmentVariable("connStr"))
                return ((note1Content |> OkObjectResult) :> IActionResult)
            }

    
// when introduced Microsoft.NET.Sdk.Functions v1.0.27 (together with evernote-cloud-sdk-windows) , run dotnet build will fail with:
//   Metadata generation failed. Exit code: '-1073741571' Output: '' Error: 'Process is terminating due to StackOverflowException.'
//
// As of the time being, there is a workaround tracked in GitHub:
// https://github.com/Azure/azure-functions-host/issues/4055#issuecomment-489143039


