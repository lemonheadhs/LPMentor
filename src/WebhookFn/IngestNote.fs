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
    let parseParams (req: HttpRequest) =
        let queryDict = req.Query |> Seq.map (fun i -> i.Key, i.Value) |> dict
        let WebhookParamCtr userId guid reason notebookGuid =
            { UserId = userId; Guid_ = guid; Reason = reason; NotebookGuid = notebookGuid }
        ()

    [<FunctionName "IngestNote">]
    let Run(
            [<HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*any}")>] req: HttpRequest,
            log: ILogger) =
    
        log.LogInformation("F# HTTP trigger function processed a request.")

        let name = req.Query.["name"]
        let note1Content = FetchNoteContent "e93720a3-91f0-4503-82e7-d125256a7cc5" |> markupInnerText

        task {
            do! noteQueue.Enqueue (note1Content, connectionString= Environment.GetEnvironmentVariable("connStr"))
            return (
                if true then    

                    (note1Content |> OkObjectResult) :> IActionResult
                else
                    BadRequestObjectResult ("Please pass a name on the query string or in the request body") :> IActionResult)
        }

    
// when introduced Microsoft.NET.Sdk.Functions v1.0.27 (together with evernote-cloud-sdk-windows) , run dotnet build will fail with:
//   Metadata generation failed. Exit code: '-1073741571' Output: '' Error: 'Process is terminating due to StackOverflowException.'
//
// As of the time being, there is a workaround tracked in GitHub:
// https://github.com/Azure/azure-functions-host/issues/4055#issuecomment-489143039


