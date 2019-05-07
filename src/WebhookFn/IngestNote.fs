module IngestNote

open System
open System.Net
open System.Net.Http
open FSharp.Control.Tasks.V2
open Microsoft.Azure.WebJobs.Host

// public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
let Run(
        req: HttpRequestMessage, log: TraceWriter) =

    log.Info("F# HTTP trigger function processed a request.")

    // parse query parameter
    let name = 
        req.GetQueryNameValuePairs()
        |> Seq.find (fun q -> String.Compare(q.Key, "name", true) = 0)
        |> fun q -> q.Value

    task {
        return (
            if isNull name then
                req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            else
                req.CreateResponse(HttpStatusCode.OK, "Hello " + name))        
    }

