#load "../../.paket/load/net472/PoC_AzureSpeech/poc_azurespeech.group.fsx"

open FSharp.Azure.StorageTypeProvider
open FSharp.Azure.StorageTypeProvider.Blob
open Microsoft.WindowsAzure.Storage.Blob

// Get a handle to my local storage emulator
type Local = AzureTypeProvider<"UseDevelopmentStorage=true",autoRefresh=5>

let test = Local.Containers.audios

test.``sql2017.txt``

Local.Tables.test

// let container = Local.Containers.CloudBlobClient.GetContainerReference("folder")
let container = Local.Containers.audios.AsCloudBlobContainer()

container.GetBlockBlobReference("sql2017.txt")
         .Exists()

FSharp.Azure.StorageTypeProvider.Blob.ContainerBuilder.createBlobClient

// At this point of time, I'm not able to make AzureTypeProvider work perfectly in fsx file,
// if I choose v2.0.1, fsx file can not get proper intellesense for generated types;
// if I fall back to v1.9.5, once I try to cast Containers to basic Azure SDK types, fsi complains about can not find methods in the assembly.
//
// In the end I have to create a console app to try out Azure TP's functionality.

