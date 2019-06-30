module LPMentor.Web.Type

open System
open Microsoft.Azure.Cosmos.Table

type AppSetting = {
    AzureStorageConnStr: string
}

type AudioEntity (partition:string, rowId:string) =
    inherit TableEntity (partition, rowId)
    member val Text = "" with get, set
    member val Topic = "" with get, set
    member val Section = "" with get, set
    member val Order = Int32.MaxValue with get, set
    member val Lang = "en" with get, set
    member val BlobName = "" with get, set
    new () = AudioEntity("v1", "")
