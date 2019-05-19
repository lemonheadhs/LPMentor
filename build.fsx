#r "paket:
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.IO.Zip
nuget Fake.Azure.Kudu
nuget Fake.DotNet.Cli
nuget FSharp.Data //"
#load "./.fake/build.fsx/intellisense_lazy.fsx"

open System.IO
open FSharp.Data
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators
open Fake.Azure

System.Environment.CurrentDirectory = __SOURCE_DIRECTORY__
printfn __SOURCE_DIRECTORY__

let funcProject = "./src/LPMentor/LPMentor.fsproj"
let zipPackageDir = "./zipTemp" |> Path.GetFullPath
let buildOutputDir = "./src/LPMentor/bin/Release" |> Path.GetFullPath

Target.create "Clean"
    (fun _ -> Shell.cleanDirs [ buildOutputDir; zipPackageDir ])

Target.create "Build" (fun _ ->
    let setParams (defaults : DotNet.PublishOptions) =
        { defaults with Configuration = DotNet.BuildConfiguration.Release
                        Framework = Some "netcoreapp2.1"
                        OutputPath = Some buildOutputDir }
    DotNet.publish setParams funcProject)
    
Target.create "Zip"
    (fun _ ->
    !!Path.Combine(buildOutputDir, "**/*")
    |> Zip.createZip buildOutputDir (Path.Combine(zipPackageDir, "publish.zip"))
           "" Zip.DefaultZipLevel false)

type PubProfile = JsonProvider<"./samples/PubProfileSample.json", ResolutionFolder=__SOURCE_DIRECTORY__>

let pubSettings =
    PubProfile.Load(Path.combine __SOURCE_DIRECTORY__ "samples/PubProfile.json")

Target.create "Deploy" (fun _ ->
    let zipPath = Path.Combine(zipPackageDir, "publish.zip")
    pubSettings
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
    printfn "All App Services are updated!")

open Fake.Core.TargetOperators

"Clean" 
    ==> "Build" 
    ==> "Zip" 
    ==> "Deploy"

Target.runOrDefault "Deploy"
