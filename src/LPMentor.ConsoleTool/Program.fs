// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open System.IO
open System.Reflection
open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Queue

open LPMentor.Core.WebhookFn.EvernoteAuth


let cwd = Assembly.GetEntryAssembly().Location |> Path.GetDirectoryName

[<Literal>]
let configSample = """{
    "azureStorageConnStr": "UseDevelopmentStorage=true",
    "consumerKey": "key",
    "consumerSecret": "secret"
}"""
type Config = JsonProvider<configSample>
let config = Path.Combine(cwd, "appsettings.json") |> Config.Load

// set config val into environment variables, because evernote auth functions rely on that
Environment.SetEnvironmentVariable ("consumerKey", config.ConsumerKey)
Environment.SetEnvironmentVariable ("consumerSecret", config.ConsumerSecret)

module Queue = begin
    let callbackQueue =
        CloudStorageAccount.Parse(config.AzureStorageConnStr)
                           .CreateCloudQueueClient()
                           .GetQueueReference("lpauthcallback")
end

module Table = begin
    open Microsoft.Azure.Cosmos.Table

    let credTable =
        CloudStorageAccount.Parse(config.AzureStorageConnStr)
                           .CreateCloudTableClient()
                           .GetTableReference("LPCredential")

    type CredEntity (partition:string, rowId:string) =
        inherit TableEntity (partition, rowId)
        member val Host = "" with get, set
        member val EdamUserId = "" with get, set
        member val NoteStoreUrl = "" with get, set
        member val WebApiUrlPrefix = "" with get, set
        member val AuthenticationToken = "" with get, set
        member val ExpirationDate = DateTime() with get, set
    with
        static member ModifyWith (ni: LENCredentials) (e: CredEntity) =
            e.Host <- ni.Host
            e.EdamUserId <- ni.EdamUserId
            e.NoteStoreUrl <- ni.NoteStoreUrl
            e.WebApiUrlPrefix <- ni.WebApiUrlPrefix
            e.AuthenticationToken <- ni.AuthenticationToken
            e.ExpirationDate <- ni.ExpirationDate
            e
        static member Save (ni: LENCredentials) =
            let e = 
                CredEntity("v1", SessionHost)
                |> CredEntity.ModifyWith (ni)
            let table = credTable
            e
            |> TableOperation.InsertOrReplace
            |> table.ExecuteAsync
end

[<EntryPoint>]
[<STAThread>]
let main argv =
    printfn "Console tool for LPMentor renew authToken"
    authenticateToEvernote() |> function
    | None ->
        printfn "fail to authenticate to Evernote.."
    | Some cred ->
        Table.CredEntity.Save(cred).Wait()
        DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
        |> CloudQueueMessage
        |> Queue.callbackQueue.AddMessage
        printfn "Refreshed Cred saved!"
    printfn "Done.."
    0 // return an integer exit code
