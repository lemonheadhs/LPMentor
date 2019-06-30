module LPMentor.Web.Lesson

open System
open Microsoft.Azure.Cosmos.Table
open FSharp.Control.Tasks.V2

open LPMentor.Web.Type

module TableQuery = begin
    type FilterCondition = FilterCondition of string
        with static member Empty = FilterCondition ""
             static member (+) (a: FilterCondition, b: FilterCondition) = 
                match a, b with
                | FilterCondition "", _ -> b
                | _, FilterCondition "" -> a
                | FilterCondition astr, FilterCondition bstr ->
                    TableQuery.CombineFilters(astr, TableOperators.And, bstr)
                    |> FilterCondition
             static member (/) (a, b) =
                match a, b with
                | FilterCondition "", _ -> b
                | _, FilterCondition "" -> a
                | FilterCondition astr, FilterCondition bstr ->
                    TableQuery.CombineFilters(astr, TableOperators.Or, bstr)
                    |> FilterCondition

    let genOperator operation a b =
        TableQuery.GenerateFilterCondition(a, operation, b)
        |> FilterCondition

    let (==) = genOperator QueryComparisons.Equal
    let (!=) = genOperator QueryComparisons.NotEqual
    let (^>) = genOperator QueryComparisons.GreaterThan
    let (^<) = genOperator QueryComparisons.LessThan
    let (^>=) = genOperator QueryComparisons.GreaterThanOrEqual
    let (^<=) = genOperator QueryComparisons.LessThanOrEqual
    
end

open TableQuery

type TableQuery<'t> with
    member x.Where (c: FilterCondition) = 
        match c with
        | FilterCondition str ->
            x.Where(c.ToString())

let EndingChar = 
    (int 'z') + 1 |> Convert.ToChar |> Convert.ToString

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
