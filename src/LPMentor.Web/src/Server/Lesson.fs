module LPMentor.Web.Lesson

open System
open Microsoft.Azure.Cosmos.Table
open FSharp.Control.Tasks.V2

open LPMentor.Web.Type
open LPMentor.Core
open LPMentor.Core.TableQuery


let internal search connStr prevToken conditions =
    let table =
        CloudStorageAccount.Parse(connStr)
                           .CreateCloudTableClient()
                           .GetTableReference("LPAudio")
    let rangeQuery =
        table.CreateQuery<AudioEntity>()
             .Where(
                   ("PartitionKey" == "v1") + conditions)

    rangeQuery.TakeCount <- Nullable 20
    task {
        let! segment = table.ExecuteQuerySegmentedAsync(rangeQuery, prevToken)
        return segment.Results, segment.ContinuationToken
    }

let searchLessons connStr (s: string, prevToken) =
      ("Topic" ^>= s ) 
    + ("Topic" ^< (s+EndingChar))
    |> search connStr prevToken

let browseAmongAllLessons connStr prevToken =
    search connStr prevToken FilterCondition.Empty
