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
    "consumerSecret": "secret",
    "subscriptionKey": "abc",
    "connStr": "connStr"
}"""
type Config = JsonProvider<configSample>
let config = Path.Combine(cwd, "appsettings.json") |> Config.Load

// set config val into environment variables, because evernote auth functions rely on that
Environment.SetEnvironmentVariable ("consumerKey", config.ConsumerKey)
Environment.SetEnvironmentVariable ("consumerSecret", config.ConsumerSecret)
Environment.SetEnvironmentVariable ("connStr", config.ConnStr)
Environment.SetEnvironmentVariable ("subscriptionKey", config.SubscriptionKey)

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

open Argu
open LPMentor.Core.Models
open System.Threading
open System.Collections.Generic
[<AutoOpen>]
module Commands = begin
// lpm audio meta -text -file | output: metadata
// lpm audio create -text -file | output: metadata, portions list
// lpm audio merge --local-only | output: metadata, merged file
// lpm audio upload
// lpm catalog update
// lpm auth 

    type LPMArgus =
        | [<AltCommandLine("-t"); Inherit>] Text of string
        | [<AltCommandLine("-f"); Inherit>] File of string
        | [<CliPrefix(CliPrefix.None)>] Audio of ParseResults<AudioArgs>
        | [<CliPrefix(CliPrefix.None)>] Catalog of ParseResults<CatalogArgs>
        | [<CliPrefix(CliPrefix.None)>] Auth
    with
        interface IArgParserTemplate with
            member s.Usage = s |> function
                | Audio _ -> "generate audios by sending note content to TTS"
                | Catalog _ -> "update audio files catalog info"
                | Auth -> "refresh evernote auth token"
                | Text _ -> "the text content of note"
                | File _ -> "the file path of note"
    and AudioArgs =
        | [<CliPrefix(CliPrefix.None)>] Meta
        | [<CliPrefix(CliPrefix.None)>] Create
        | [<CliPrefix(CliPrefix.None)>] Merge of ParseResults<MergeArgs>
        | [<CliPrefix(CliPrefix.None)>] Upload of ParseResults<UploadArgs>
    with
        interface IArgParserTemplate with
            member s.Usage = s |> function
                | Meta -> "inspect meta info in note content"
                | Create -> "create audio portion files"
                | Merge _ -> "merge audio portion files (the push file)"
                | Upload _ -> "upload local audio file"
    and MergeArgs =
        | [<AltCommandLine("-lo")>] Local_Only
        | [<AltCommandLine("-lp")>] Local_Push
    with 
        interface IArgParserTemplate with
            member s.Usage = s |> function
                | Local_Only -> "save to local file system only"
                | Local_Push -> "save to local and push to cloud"
    and UploadArgs =
        | [<AltCommandLine("-src")>] Source of string
    with 
        interface IArgParserTemplate with
            member s.Usage = s |> function
                | Source _ -> "the path of the local audio file"
    and CatalogArgs =
        | [<CliPrefix(CliPrefix.None); Mandatory>] Update
    with
        interface IArgParserTemplate with
            member s.Usage = s |> function
                | Update -> "update audio files catalog info"

    open LPMentor.Core.WebhookFn.Evernote
    open LPMentor.Core.Activities
    open FSharp.Control.Tasks.V2
    open System.Threading.Tasks

    let writeRedLine (s:string) =
        let preColor = Console.ForegroundColor
        Console.ForegroundColor <- ConsoleColor.Red
        Console.WriteLine s
        Console.ForegroundColor <- preColor

    let metadataOf (results:ParseResults<LPMArgus>) =
        let tryParseFromOptionText (s: string option) = 
            s |> Option.bind (fun txt -> 
                tryParseAudioNoteMetadata txt 
                |> Option.map (fun meta -> meta, txt))
        let metadata =
            results.TryGetResult Text
            |> tryParseFromOptionText
            |> Option.orElse (
                results.TryGetResult File
                |> Option.bind (fun path -> 
                    if File.Exists path 
                    then File.ReadAllText path |> Some 
                    else None
                    |> tryParseFromOptionText))
        if Option.isSome metadata then
            printfn "%A" (fst (Option.get metadata))
        else
            writeRedLine "can not find text or file, or meta data formate is inalid"            
        metadata

    let storeLocal filename (stream:Stream) =
        task {
            use fs = File.Create filename
            do! stream.CopyToAsync fs
        } :> Task

    let handleAudioCreate (meta: AudioNoteMetadata) (content:string) =
        let segments = splitTextSize 1000 content |> Seq.toArray
        let segmentNames = 
            [1..segments.Length]
            |> List.map (sprintf "%s/portions/%i_%s_%i.mp3"
                                meta.Topic
                                meta.Order
                                meta.Section)
            |> List.toArray
        let semaphore = new SemaphoreSlim(5,5)
        printfn "%i segments" segments.Length

        let stack = Stack<int>([0..(segments.Length - 1)])
        let runOne i =
            task {
                do! semaphore.WaitAsync()
                let filename = segmentNames.[i]
                printfn "start generating %s .." filename
                try
                    try
                        let fullname = Path.GetFullPath filename
                        let dir = Path.GetDirectoryName fullname
                        if not <| Directory.Exists dir then Directory.CreateDirectory dir |> ignore
                        let! name =
                            struct (fullname, meta.Lang, segments.[i])
                            |> genAudioFileThen storeLocal
                        printfn "done %s" filename
                    with | e ->
                        stack.Push(i)
                        writeRedLine(e.Message)
                        if not <| isNull e.InnerException then
                            writeRedLine(e.InnerException.Message)
                            e.InnerException.StackTrace |> writeRedLine
                        else writeRedLine(e.StackTrace)
                finally
                    semaphore.Release() |> ignore
            } |> ignore
        task {
            let mutable r = 0
            while (stack.Count > 0 || semaphore.CurrentCount < 3) do
                let n = stack.Count
                for i in [1..n] do
                    r <- r + 1
                    if r > segments.Length then printfn "shoot %i .." r
                    stack.Pop() |> runOne
                do! Task.Delay 250
        } |> fun t -> t.Wait()

    let trackInfo meta content mergedFileName =
        let noteInfo: NoteInfo = {
            Text = content
            Topic = meta.Topic
            Section = meta.Section
            Order = meta.Order
            Lang = meta.Lang
        }
        storeAudioInfo_ struct(noteInfo, mergedFileName)

    let handleAudioMerge (meta: AudioNoteMetadata) (content:string) (mergeArg:ParseResults<MergeArgs>) =
        let segments = splitTextSize 1000 content |> Seq.toList
        let segmentNames = 
            [1..segments.Length]
            |> List.map (sprintf "%s/portions/%i_%s_%i.mp3"
                                meta.Topic
                                meta.Order
                                meta.Section)
        let mergedFileName = 
            sprintf "%s/%i_%s.mp3" 
                    meta.Topic 
                    meta.Order 
                    meta.Section
        let thenTrackInfo fn x =
            task {
                do! fn x
                do! trackInfo meta content mergedFileName
            }
        let mergeInMemThen (fn:string -> Stream -> Task) 
                   struct(files:string list, mergedFileName:string) =
            task {
                if files |> Seq.forall (File.Exists) then
                    let ms = new MemoryStream()
                    for file in files do
                        use fs = File.OpenRead file
                        do! fs.CopyToAsync ms
                    ms.Position <- 0L
                    do! (fn mergedFileName ms)
                    ms.Dispose()
            }
        match mergeArg with
        | args when args.Contains Local_Push ->
            mergeInMemThen (fun file ms -> 
                task {
                    do! storeLocal file ms
                    ms.Position <- 0L
                    do! pushFile file ms
                }:> Task) |> thenTrackInfo
        | _ -> 
            mergeInMemThen storeLocal
        |> fun doMerge -> doMerge struct(segmentNames, mergedFileName)
        |> fun t -> t.Wait()

    let handleAudioUpload (meta: AudioNoteMetadata) (content:string) (uploadArg:ParseResults<UploadArgs>) =
        let mergedFileName = 
            sprintf "%s/%i_%s.mp3" 
                    meta.Topic 
                    meta.Order 
                    meta.Section
        uploadArg.TryGetResult UploadArgs.Source
        |> Option.bind (fun src ->
            if not <| File.Exists src
            then None
            else File.OpenRead src |> Some)
        |> Option.map (fun fs ->
            task {
                do! pushFile mergedFileName fs 
                do! trackInfo meta content mergedFileName
                fs.Dispose()
            })
        |> function
        | Some t -> t.Wait()
        | None -> 
            printfn "unexpected case of audio upload command; src file not specified or not found"; 
            printfn "%s" (uploadArg.Parser.PrintUsage())

    let handleAudioCmds someMeta (audioArgs:ParseResults<AudioArgs>) =
        if Option.isSome someMeta then
            let (meta: AudioNoteMetadata, content:string) = Option.get someMeta
            match audioArgs with
            | args when args.Contains Meta -> "do nothing here" |> ignore
            | args when args.Contains AudioArgs.Create -> 
                handleAudioCreate meta content
            | args when args.Contains Merge -> 
                handleAudioMerge meta content (args.GetResult Merge)
            | args when args.Contains Upload -> 
                handleAudioUpload meta content (args.GetResult Upload)
            | args -> printfn "unexpected case of audio command"; printfn "%s" (args.Parser.PrintUsage())

    let handleCatalogCmds someMeta (catalogArgs:ParseResults<CatalogArgs>) =
        if Option.isSome someMeta then
            let (meta: AudioNoteMetadata, content:string) = Option.get someMeta
            match catalogArgs with
            | args when args.Contains CatalogArgs.Update -> 
                updateCatalog_ meta.Topic |> fun t -> t.Wait()
            | args -> printfn "unexpected case of catalog command"; printfn "%s" (args.Parser.PrintUsage())

    let handleAuthCmds () =
        authenticateToEvernote() |> function
        | None ->
            printfn "fail to authenticate to Evernote.."
        | Some cred ->
            Table.CredEntity.Save(cred).Wait()
            DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            |> CloudQueueMessage
            |> Queue.callbackQueue.AddMessage
            printfn "Refreshed Cred saved!"

end

[<EntryPoint>]
[<STAThread>]
let main argv =
    printfn "Console tool for LPMentor renew authToken"
    let parser = ArgumentParser.Create<LPMArgus>(programName = "lpm.exe")
    let results = parser.Parse argv

    match results with
    | rs when rs.Contains Audio -> handleAudioCmds (metadataOf rs) (rs.GetResult Audio)
    | rs when rs.Contains Catalog -> handleCatalogCmds (metadataOf rs) (rs.GetResult Catalog)
    | rs when rs.Contains Auth -> handleAuthCmds ()
    | _ -> printfn "%s" (parser.PrintUsage())
    0 // return an integer exit code
