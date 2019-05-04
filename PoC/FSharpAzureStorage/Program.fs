// Learn more about F# at http://fsharp.org

open System
open FSharp.Azure.StorageTypeProvider
open FSharp.Azure.StorageTypeProvider.Blob
open System.IO
open System.IO


// As of today May 2019, I have to tweak the proj file a little bit to make it compile.
// https://github.com/Microsoft/visualfsharp/issues/6326

type Local = AzureTypeProvider<"UseDevelopmentStorage=true",autoRefresh=5>
let AudiosContainer = Local.Containers.audios

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    let container = AudiosContainer.AsCloudBlobContainer()
    // container.GetBlockBlobReference("sql2017.txt")
    //          .DownloadText()
    // |> printfn "file content: %s"

    use filestm = new FileStream("testfile.txt", FileMode.Open)
    container.GetBlockBlobReference("folder/newfile.txt")
             .UploadFromStream(filestm)

    Console.ReadLine()  |> ignore
    0 // return an integer exit code
