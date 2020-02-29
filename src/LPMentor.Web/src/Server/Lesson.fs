module LPMentor.Web.Lesson

open System
open Microsoft.Azure.Cosmos.Table
open FSharp.Control.Tasks.V2
open Newtonsoft.Json
open Microsoft.Extensions.Options

open LPMentor.Web.Type
open LPMentor.Core.AzTables
open LPMentor.Core.AzTables.TableQuery
open Shared


let internal search connStr prevToken conditions =
    let table =
        CloudStorageAccount.Parse(connStr)
                           .CreateCloudTableClient()
                           .GetTableReference("LPCatalog")
    let rangeQuery =
        TableQuery<CatalogEntity>()
             .Where(
                   ("PartitionKey" == "v1") + conditions)

    rangeQuery.TakeCount <- Nullable 20
    task {
        let! segment = table.ExecuteQuerySegmentedAsync(rangeQuery, prevToken)
        return segment.Results, segment.ContinuationToken
    }

let searchLessons connStr (s: string) (prevToken:TableContinuationToken) =
      ("RowKey" ^>= s ) 
    + ("RowKey" ^< (s+EndingChar))
    |> search connStr prevToken

let browseAmongAllLessons connStr prevToken =
    search connStr prevToken FilterCondition.Empty

let lessonApi = reader {
    let! appSettingsOption = resolve<IOptionsSnapshot<AppSetting>> ()
    let appSettings = appSettingsOption.Value
    let connStr = appSettings.AzureStorageConnStr
    let fromTableToken (t:TableToken) =
        if String.IsNullOrEmpty t.Token then null
        else JsonConvert.DeserializeObject<TableContinuationToken> (t.Token)
    let toTableToken (ct:TableContinuationToken) =
        { Token = JsonConvert.SerializeObject ct }
    let adapt (x: Async<ResizeArray<CatalogEntity>*TableContinuationToken>) = async {
        let! ls, ct = x
        let lessons = ls.ToArray() |> Array.map (fun l -> { Topic = l.RowKey; Sections = JsonConvert.DeserializeObject<Section []>(l.Summary) })
        return lessons, (toTableToken ct)
    }
    return {
        init = fromTableToken >> browseAmongAllLessons connStr >> Async.AwaitTask >> adapt
        searchTopic = fun topic ttoken -> fromTableToken ttoken |> searchLessons connStr topic |> Async.AwaitTask |> adapt }
}
