namespace LPMentor.Durable

open System
open System.Net.Http
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

        task {
            return
                Evernote.parseParams req |> function
                | None -> BadRequestObjectResult ("Please pass a name on the query string or in the request body") :> IActionResult
                | Some webhookParams ->
                    ("Success!" |> OkObjectResult) :> IActionResult
        }



