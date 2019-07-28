#load "../../.paket/load/net472/Durable/Newtonsoft.Json.fsx"
#load "../../.paket/load/net472/Durable/Microsoft.Azure.Storage.Blob.fsx"
#load "../../.paket/load/net472/Durable/Microsoft.Azure.Cosmos.Table.fsx"
#load "../../.paket/load/net472/Durable/TaskBuilder.fs.fsx"
//#load "../../src/LPMentor.Core/Model.fs"
#r "../../src/LPMentor.Core/bin/Debug/netcoreapp2.1/LPMentor.Core.dll"
#load "../../src/LPMentor.Web/src/server/TableQuery.fs"
#load "../../src/LPMentor.Durable/Storage.fs"

open System

Environment.SetEnvironmentVariable("connStr", "UseDevelopmentStorage=true")

open LPMentor.Durable.Storage.Table

CatalogEntity.Recollect "test"

|> CatalogEntity.Save





