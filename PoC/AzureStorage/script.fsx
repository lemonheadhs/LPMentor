#load "../../.paket/load/net472/Durable/Microsoft.Azure.Storage.Blob.fsx"

open System
open System.IO
open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob

let connStr = "UseDevelopmentStorage=true"
let filePaths = 
    ["merge_test/001.txt";"merge_test/002.txt"]

let getAudioContainer () =
    CloudStorageAccount.Parse(connStr)
                       .CreateCloudBlobClient()
                       .GetContainerReference("audios")

let mergeFiles (files:string seq) mergedFileName =
    let container = getAudioContainer ()
    use ms = MemoryStream()
    files
    |> Seq.iter (fun f ->
                    printfn "%s" f
                    container.GetBlockBlobReference(f)
                             .DownloadToStream (ms))    
    ms.Position <- 0L

    // use fi = File.Create("merged2.txt")
    // ms.CopyTo(fi)
    // fi.Flush()
    container.GetBlockBlobReference(mergedFileName)
             .UploadFromStream(ms)

"merged.txt"
|> mergeFiles filePaths

