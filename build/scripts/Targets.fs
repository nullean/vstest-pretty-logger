module Targets

open Argu
open System
open System.IO
open Bullseye
open CommandLine
open Fake.Tools.Git
open ProcNet

    
let exec binary args =
    Proc.Exec (binary, args |> List.toArray)
    
let private restoreTools = lazy(exec "dotnet" ["tool"; "restore"])
let private currentVersion =
    lazy(
        restoreTools.Value |> ignore
        let r = Proc.Start("dotnet", "minver", "-p", "canary.0", "-m", "0.1")
        let o = r.ConsoleOut |> Seq.find (fun l -> not(l.Line.StartsWith("MinVer:")))
        o.Line
    )
let private currentVersionInformational =
    lazy(
        match Paths.IncludeGitHashInInformational with
        | false -> currentVersion.Value
        | true -> sprintf "%s+%s" currentVersion.Value (Information.getCurrentSHA1( "."))
    )

let private clean (arguments:ParseResults<Arguments>) =
    if (Paths.Output.Exists) then Paths.Output.Delete (true)
    // clean every project but this build itself: `dotnet clean` on the solution would delete the assemblies the
    // running build still loads lazily (Fake, Proc) and fail the next target
    ["src"; "tests"]
    |> Seq.collect (fun d -> Directory.EnumerateFiles(d, "*.?sproj", SearchOption.AllDirectories))
    |> Seq.iter (fun p -> exec "dotnet" ["clean"; p; "-c"; "Release"; "-v"; "q"] |> ignore)
    
let private build (arguments:ParseResults<Arguments>) = exec "dotnet" ["build"; "-c"; "Release"] |> ignore

let private test (_:ParseResults<Arguments>) =
    // we don't want to test anything really :)
    // We have example tests that fail on purpose
    ignore()

let private generatePackages (arguments:ParseResults<Arguments>) =
    let output = Paths.RootRelative Paths.Output.FullName
    exec "dotnet" ["pack"; "-c"; "Release"; "-o"; output] |> ignore
    
let private validatePackages (arguments:ParseResults<Arguments>) =
    let output = Paths.RootRelative <| Paths.Output.FullName
    let nugetPackages =
        Paths.Output.GetFiles("*.nupkg") |> Seq.sortByDescending(fun f -> f.CreationTimeUtc)
        |> Seq.map (fun p -> Paths.RootRelative p.FullName)
        
    let jenkinsOnWindowsArgs =
        if Fake.Core.Environment.hasEnvironVar "JENKINS_URL" && Fake.Core.Environment.isWindows then ["-r"; "true"] else []
    
    let args = ["-v"; currentVersionInformational.Value; "-k"; Paths.SignKey; "-t"; output] @ jenkinsOnWindowsArgs
    nugetPackages |> Seq.iter (fun p -> exec "dotnet" (["nupkg-validator"; p] @ args) |> ignore)
    

let private generateApiChanges (arguments:ParseResults<Arguments>) =
    let output = Paths.RootRelative <| Paths.Output.FullName
    let currentVersion = currentVersion.Value
    let nugetPackages =
        Paths.Output.GetFiles("*.nupkg") |> Seq.sortByDescending(fun f -> f.CreationTimeUtc)
        |> Seq.map (fun p -> Path.GetFileNameWithoutExtension(Paths.RootRelative p.FullName).Replace("." + currentVersion, ""))
    nugetPackages
    |> Seq.iter(fun p ->
        let outputFile =
            let f = sprintf "breaking-changes-%s.md" p
            Path.Combine(output, f)
        let args =
            [
                "assembly-differ"
                (sprintf "previous-nuget|%s|%s|%s" p currentVersion Paths.MainTFM);
                (sprintf "directory|src/%s/bin/Release/%s" p Paths.MainTFM);
                "-a"; "true"; "--target"; p; "-f"; "github-comment"; "--output"; outputFile
            ]
        
        exec "dotnet" args |> ignore
    )
    
let Setup (parsed:ParseResults<Arguments>) (subCommand:Arguments) =
    let step (name:string) action = Targets.Target(name, new Action(fun _ -> action(parsed)))
    
    let cmd (name:string) commandsBefore steps action =
        let singleTarget = (parsed.TryGetResult SingleTarget |> Option.defaultValue false)
        let deps =
            match (singleTarget, commandsBefore) with
            | (true, _) -> [] 
            | (_, Some d) -> d
            | _ -> []
        let steps = steps |> Option.defaultValue []
        Targets.Target(name, deps @ steps, Action(action))
        
    step Clean.Name clean
    cmd Build.Name None (Some [Clean.Name]) <| fun _ -> build parsed
    
    cmd Test.Name (Some [Build.Name;]) None <| fun _ -> test parsed
    
    cmd Pack.Name
        (Some [Build.Name;])
        (Some [GeneratePackages.Name; ValidatePackages.Name; GenerateApiChanges.Name])
        ignore
    cmd Ci.Name
        (Some [Test.Name;])
        (Some [GeneratePackages.Name; ValidatePackages.Name; GenerateApiChanges.Name])
        ignore

    step GeneratePackages.Name generatePackages 
    step ValidatePackages.Name validatePackages 
    step GenerateApiChanges.Name generateApiChanges
