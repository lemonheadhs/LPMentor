open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration

open FSharp.Control.Tasks.V2
open Giraffe
open Shared
open LPMentor.Web.Type

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"
let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let counterApi = {
    initialCounter = fun () -> async { return { Value = 42 } }
}

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue counterApi
    |> Remoting.buildHttpHandler


let configureAppConfig (ctx: WebHostBuilderContext) (config: IConfigurationBuilder) =
    let environment = ctx.HostingEnvironment.EnvironmentName
    config
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional = true)
        .AddJsonFile(sprintf "appsettings.%s.json" environment, optional = true)
        .AddEnvironmentVariables()
    |> ignore

let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (hostCtx: WebHostBuilderContext) (services : IServiceCollection) =
    let config = hostCtx.Configuration
    services.AddGiraffe() |> ignore
    services.Configure<AppSetting>(config) |> ignore

#if DEBUG
let contentRoot = publicPath
let webRoot = publicPath
#else
let contentRoot = Directory.GetCurrentDirectory()
let webRoot = Path.Combine(contentRoot, "Public")
#endif

[<EntryPoint>]
let main _ =
    WebHost
        .CreateDefaultBuilder()
        .UseWebRoot(webRoot)
        .UseContentRoot(contentRoot)
        .ConfigureAppConfiguration(configureAppConfig)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0
