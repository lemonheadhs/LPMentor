#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System.IO
open FSharp.Data
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators
open Fake.Azure

System.Environment.CurrentDirectory = __SOURCE_DIRECTORY__
printfn __SOURCE_DIRECTORY__

let funcProject = "./src/LPMentor.Durable/LPMentor_Durable.fsproj"
let zipPackageDir = "./zipTemp" |> Path.GetFullPath
let buildOutputDir = "./src/LPMentor.Durable/bin/Release" |> Path.GetFullPath

Target.create "Clean"
    (fun _ -> Shell.cleanDirs [ buildOutputDir; zipPackageDir ])

Target.create "Build" (fun _ ->
    let setParams (defaults : DotNet.PublishOptions) =
        { defaults with Configuration = DotNet.BuildConfiguration.Release
                        Framework = Some "netcoreapp2.1"
                        OutputPath = Some buildOutputDir }
    DotNet.publish setParams funcProject
    Shell.copy buildOutputDir ["./src/LPMentor.Durable/host.json"])
    
Target.create "Zip"
    (fun _ ->
    !!Path.Combine(buildOutputDir, "**/*")
    |> Zip.createZip buildOutputDir (Path.Combine(zipPackageDir, "publish.zip"))
           "" Zip.DefaultZipLevel false)

type PubProfile = JsonProvider<"./samples/PubProfileSample.json", ResolutionFolder=__SOURCE_DIRECTORY__>

let pubSettings =
    PubProfile.Load(Path.combine __SOURCE_DIRECTORY__ "samples/PubProfile.json")

let performDeploy (settings:PubProfile.Root[]) zipPath =
    settings
    |> Seq.iter (fun p ->
           let deployParams : Kudu.ZipDeployParams =
               { PackageLocation = zipPath
                 Password = p.GitPassword
                 Url =
                     p.GitUrl
                     |> (sprintf "https://%s")
                     |> System.Uri
                 UserName = p.GitUsername }
           Kudu.zipDeploy deployParams
           printfn "App Service %s deployed" p.Name)
    printfn "All App Services are updated!"

Target.create "Deploy" (fun _ ->
    Path.Combine(zipPackageDir, "publish.zip")
    |> performDeploy pubSettings)


open System

open Fake.Core
open Fake.DotNet
open Fake.IO

let webProjFolder = Path.getFullName "./src/LPMentor.Web"
let serverPath = Path.getFullName "./src/LPMentor.Web/src/Server"
let clientPath = Path.getFullName "./src/LPMentor.Web/src/Client"
let clientDeployPath = Path.combine clientPath "deploy"
let deployDir = Path.combine serverPath "deploy"

let release = ReleaseNotes.load (Path.combine webProjFolder "RELEASE_NOTES.md")

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    Command.ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore


Target.create "CleanWeb" (fun _ ->
    [ deployDir
      clientDeployPath
      zipPackageDir ]
    |> Shell.cleanDirs
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" webProjFolder
    printfn "Yarn version:"
    runTool yarnTool "--version" webProjFolder
    runTool yarnTool "install --frozen-lockfile" webProjFolder
)

Target.create "BuildWeb" (fun _ ->
    runDotNet (sprintf "publish -o %s -c Release" deployDir) serverPath
    Shell.regexReplaceInFileWithEncoding
        "let app = \".+\""
       ("let app = \"" + release.NugetVersion + "\"")
        System.Text.Encoding.UTF8
        (Path.combine clientPath "Version.fs")
    runTool yarnTool "webpack-cli -p" webProjFolder
)

Target.create "ZipWeb" (fun _ ->
    let webRoot = Path.combine deployDir "Public"
    Shell.mkdir webRoot
    Shell.copyRecursive clientDeployPath webRoot true |> ignore
    !!Path.Combine(deployDir, "**/*")
    |> Zip.createZip deployDir (Path.Combine(zipPackageDir, "publishWeb.zip"))
            "" Zip.DefaultZipLevel false
)

let pubWebSettings = PubProfile.Load(Path.combine __SOURCE_DIRECTORY__ "samples/PubProfile.web.json")

Target.create "DeployWeb" (fun _ ->
    Path.Combine(zipPackageDir, "publishWeb.zip")
    |> performDeploy pubWebSettings)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runTool yarnTool "webpack-dev-server" webProjFolder
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "http://localhost:8080"
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [ if not safeClientOnly then yield server
          yield client
          if not vsCodeSession then yield browser ]

    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)






open Fake.Core.TargetOperators

"CleanWeb"
    ==> "InstallClient"
    ==> "BuildWeb"
    ==> "ZipWeb"
    ==> "DeployWeb"


"CleanWeb"
    ==> "InstallClient"
    ==> "Run"


"Clean" 
    ==> "Build" 
    ==> "Zip" 
    ==> "Deploy"

Target.runOrDefault "Deploy"
