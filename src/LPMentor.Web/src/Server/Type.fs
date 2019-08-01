module LPMentor.Web.Type

open System
open Microsoft.Azure.Cosmos.Table

[<CLIMutable>]
type AppSetting = {
    AzureStorageConnStr: string
}

type CatalogEntity (partition:string, rowId:string) =
    inherit TableEntity (partition, rowId)
    member val Summary = "" with get, set
    new () = CatalogEntity("v1", "")
