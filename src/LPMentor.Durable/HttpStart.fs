namespace LPMentor.Durable

open System
open System.Net.Http
open System.Threading.Tasks
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2

open LPMentor.Core.WebhookFn

module HttpStart =

    [<FunctionName("HttpStart")>]
    let Run (
            [<HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*any}")>]
            req: HttpRequest,
            [<OrchestrationClient>]
            starter: DurableOrchestrationClient,
            log: ILogger) = 
        
        log.LogInformation("F# HTTP trigger function processed a request.")

        Evernote.parseParams req |> function
        | None -> task { return (BadRequestObjectResult ("invalid webhook req") :> IActionResult) }
        | Some webhookParams ->
            task {
                let! instanceId = starter.StartNewAsync ("NoteIngest", webhookParams)
                return ("Success!" |> OkObjectResult) :> IActionResult
            }



