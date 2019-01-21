// --------------------------------------------------------------------------------------
// Managed Injector FAKE build script
// --------------------------------------------------------------------------------------

#r "paket: groupref FakeBuild //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
  #r "netstandard"
#endif

open System
open System.IO
open System.Net

open Fake.Core
open Fake.DotNet
open Fake.Core
open Fake.IO
open Fake.Core.TargetOperators
open Fake.IO.Globbing.Operators
open System

// Install dependencies
let savedCurDir = Environment.CurrentDirectory
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
 
// The name of the project
let project = "ManagedInjector"

// Short summary of the project
let description = "A .NET Assembly process injector."

// List of author names (for NuGet package)
let authors = [ "Enkomio" ]

// Build dir
let buildDir = "./build"

// Release dir
let releaseDir = "./release"

let projects = [        
    "ES.ManagedInjector.csproj"
]

// Read additional information from the release notes document
let releaseNotesData = 
    let changelogFile = Path.Combine("..", "RELEASE_NOTES.md")
    File.ReadAllLines(changelogFile)
    |> ReleaseNotes.parseAll
    
let releaseVersion = (List.head releaseNotesData)
Trace.log("Build release: " + releaseVersion.AssemblyVersion)

let genCSAssemblyInfo (projectPath) =
    let projectName = Path.GetFileNameWithoutExtension(projectPath)
    let folderName = Path.GetFileName(System.IO.Path.GetDirectoryName(projectPath))
    let fileName = Path.Combine(folderName, "AssemblyInfo.cs")

    AssemblyInfoFile.createCSharp fileName
        [ 
            AssemblyInfo.Title projectName        
            AssemblyInfo.Product project
            AssemblyInfo.Company (authors |> String.concat ", ")
            AssemblyInfo.Description description
            AssemblyInfo.Version (releaseVersion.AssemblyVersion + ".*")
            AssemblyInfo.FileVersion (releaseVersion.AssemblyVersion + ".*")
            AssemblyInfo.InformationalVersion (releaseVersion.NugetVersion + ".*") 
        ]
        
(*
    FAKE targets
*)
Target.create "Clean" (fun _ ->
    File.delete "paket.lock"

    Shell.cleanDir buildDir
    Directory.ensure buildDir

    Shell.cleanDir releaseDir
    Directory.ensure releaseDir
)

Target.create "SetAssemblyInfo" (fun _ ->
    !! "*/ES.ManagedInjector/*.csproj"
    |> Seq.iter genCSAssemblyInfo    
)

Target.create "Compile" (fun _ ->
    let setParams (defaults:MSBuildParams) =
        { defaults with
            Verbosity = Some(Quiet)
            Targets = ["Build"]
            Properties =
                [
                    "DebugSymbols", "True"
                    "SolutionDir", __SOURCE_DIRECTORY__ + @"\"
                ]
         }

    let build(project: String, buildDir: String) =
        Trace.log("Compile: " + project)
        let fileName = Path.GetFileNameWithoutExtension(project)
        let buildAppDir = Path.Combine(buildDir, fileName)
        Directory.ensure buildAppDir

        // build the project
        [project]
        |> MSBuild.runRelease setParams buildAppDir "Build"
        |> Trace.logItems "AppBuild-Output: "

    // build all projects
    projects
    |> List.map(fun projName ->
        let projDir = Path.GetFileNameWithoutExtension(projName)
        let projFile = Path.Combine(projDir, projName)
        Trace.log("Build project: " + projFile)
        projFile
    )
    |> List.iter(fun projectFile -> build(projectFile, buildDir))
)

Target.create "Release" (fun _ ->
    let forbidden = [".pdb"]    
    let forbiddenFiles = ["RGiesecke.DllExport.Metadata.dll"]

    !! (buildDir + "/**/*.*")         
    |> Seq.filter(fun f -> 
        forbidden 
        |> List.contains (Path.GetExtension(f).ToLowerInvariant())
        |> not
    )
    |> Seq.filter(fun f -> 
        forbiddenFiles
        |> List.exists(fun forbiddenFile -> Path.GetFileName(f).Equals(forbiddenFile, StringComparison.OrdinalIgnoreCase))
        |> not
    )
    |> Zip.zip buildDir (Path.Combine(releaseDir, "ManagedInjector.v" + releaseVersion.AssemblyVersion + ".zip"))
)

Target.description "Default Build all artifacts"
Target.create "Default" ignore

(*
    Run task
*)

"Clean"
    ==> "SetAssemblyInfo"
    ==> "Compile"
    ==> "Release"
    ==> "Default"

// start build
Target.runOrDefault "Default"

// restore directory
Environment.CurrentDirectory <- savedCurDir