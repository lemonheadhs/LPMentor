namespace LPMentor.WebhookFn

open System
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open FSharp.Azure.StorageTypeProvider.Table
open LPMentor.Storage

open LPMentor.Core.WebhookFn
open LPMentor.Core.Models

module IngestNote =

    [<Obsolete>]
    [<FunctionName "IngestNote">]
    let Run(
            [<HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*any}")>]
            req: HttpRequest,
            log: ILogger) =
    
        log.LogInformation("F# HTTP trigger function processed a request.")

        match Evernote.parseParams req with
        | None -> task { return (BadRequestObjectResult ("Please pass a name on the query string or in the request body") :> IActionResult) }
        | Some webhookParams ->

            match Evernote.FetchNoteContent webhookParams.Guid_ with
            | None -> task { return (BadRequestObjectResult ("Can not find valid audio note") :> IActionResult) }
            | Some (note1Content, metadata) ->
                
                let note: NoteInfoEx = {
                    Partition = "v1"
                    RowId = sprintf "%s_%i_%s" metadata.Topic metadata.Order metadata.Section
                    BaseInfo = {
                        Text = note1Content
                        Topic = metadata.Topic
                        Section = metadata.Section
                        Order = metadata.Order
                        Lang = metadata.Lang
                    }
                }
                
                task {
                    let connStr = Environment.GetEnvironmentVariable("connStr")
                    do! lpnote.InsertAsync(Partition note.Partition, Row note.RowId, note.BaseInfo, connectionString= connStr) |> Async.Ignore
                    do! noteQueue.Enqueue (sprintf "%s##%s" note.Partition note.RowId, connectionString= connStr)
                    return (("Success!" |> OkObjectResult) :> IActionResult)
                }

    
// when introduced Microsoft.NET.Sdk.Functions v1.0.27 (together with evernote-cloud-sdk-windows) , run dotnet build will fail with:
//   Metadata generation failed. Exit code: '-1073741571' Output: '' Error: 'Process is terminating due to StackOverflowException.'
//
// As of the time being, there is a workaround tracked in GitHub:
// https://github.com/Azure/azure-functions-host/issues/4055#issuecomment-489143039


