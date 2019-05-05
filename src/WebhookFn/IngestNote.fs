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


module IngestNote =

    [<FunctionName "IngestNote">]
    let Run(
            [<HttpTrigger(AuthorizationLevel.Anonymous, Route = "{*any}")>] req: HttpRequest,
            log: ILogger) =
    
        log.LogInformation("C# HTTP trigger function processed a request.")

        let name = req.Query.["name"]

        task {
            let! requestBody = (StreamReader(req.Body)).ReadToEndAsync()
            let data = JsonConvert.DeserializeObject(requestBody)

            return (
                if not <| isNull(data) then
                    (sprintf "Hello, %s" (data.ToString()) |> OkObjectResult) :> IActionResult
                else
                    BadRequestObjectResult ("Please pass a name on the query string or in the request body") :> IActionResult)
        }

    


