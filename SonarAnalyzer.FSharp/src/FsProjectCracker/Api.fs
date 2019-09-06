module FsProjectCracker.Api

open Attempt
open System.IO
open Dotnet.ProjInfo.Inspect
open MSBuild
open System
open FsProjectCracker.Domain


let uninstall_old_target_file log (projPath:string) =
    let projDir, projName = Path.GetDirectoryName(projPath), Path.GetFileName(projPath)
    let objDir = Path.Combine(projDir, "obj")
    let targetFileDestPath = Path.Combine(objDir, (sprintf "%s.proj-info.targets" projName))

    log (sprintf "searching deprecated target file in '%s'." targetFileDestPath)
    if File.Exists targetFileDestPath then
        log (sprintf "found deprecated target file in '%s', deleting." targetFileDestPath)
        File.Delete targetFileDestPath

let getNewTempFilePath suffix =
    let outFile = System.IO.Path.GetTempFileName()
    if File.Exists outFile then File.Delete outFile
    sprintf "%s.%s" outFile suffix

let writeTargetFile log templates targetFileDestPath =
    // https://github.com/dotnet/cli/issues/5650

    let targetFileTemplate =
        """
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<Project ToolsVersion="14.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <MSBuildAllProjects>$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>
  </PropertyGroup>
        """
        + (templates |> String.concat (System.Environment.NewLine))
        +
        """
</Project>
        """

    let targetFileOnDisk =
        if File.Exists(targetFileDestPath) then
            try
                Some (File.ReadAllText targetFileDestPath)
            with
            | _ -> None
        else
            None

    let newTargetFile = targetFileTemplate.Trim()

    if targetFileOnDisk <> Some newTargetFile then
        log (sprintf "writing helper target file in '%s'" targetFileDestPath)
        File.WriteAllText(targetFileDestPath, newTargetFile)

    Ok targetFileDestPath

let getProjectInfo log msbuildExec getArgs additionalArgs (projPath: string) =
    //TODO refactor to use getProjectInfosOldSdk
    let template, args, parse =  getArgs ()

    // remove deprecated target file, if exists
    projPath
    |> uninstall_old_target_file log

    getNewTempFilePath "proj-info.hook.targets"
    |> writeTargetFile log [template]
    |> Result.bind (fun targetPath -> msbuildExec projPath (args @ additionalArgs @ [ Property("CustomAfterMicrosoftCommonTargets", targetPath) ]))
    |> Result.bind (fun _ -> parse ())

let analizeProj projPath = attempt {

    let! (isDotnetSdk, pi) =
        match projPath with
        | ProjectRecognizer.DotnetSdk pi ->
            Ok (true, pi)
        | ProjectRecognizer.OldSdk pi ->
            Ok (false, pi)
        | ProjectRecognizer.Unsupported ->
            Errors.GenericError "unsupported project format"
            |> Result.Error

    return isDotnetSdk, pi, getProjectInfo
    }



let fscArgsMain log (projPath:string) (cliArgs:CliArgs ) = attempt {

    let! (isDotnetSdk, pi, getProjectInfoBySdk) = analizeProj projPath

    let! getCompilerArgsBySdk =
        match isDotnetSdk, pi.Language with
        | true, ProjectRecognizer.ProjectLanguage.FSharp ->
            Ok getFscArgs
        | false, ProjectRecognizer.ProjectLanguage.FSharp ->
            let asFscArgs props =
                let fsc = Microsoft.FSharp.Build.Fsc.Create()
                Dotnet.ProjInfo.FakeMsbuildTasks.getResponseFileFromTask props fsc
            Ok (getFscArgsOldSdk (asFscArgs >> Ok))
        | _, ProjectRecognizer.ProjectLanguage.CSharp ->
            Errors.GenericError (sprintf "fsc args not supported on .csproj, expected an .fsproj" )
            |> Result.Error
        | _, ProjectRecognizer.ProjectLanguage.Unknown ext ->
            Errors.GenericError (sprintf "compiler args not supported on project with extension %s, expected .fsproj" ext)
            |> Result.Error

    let globalArgs =
        [ cliArgs.Framework, (if isDotnetSdk then "TargetFramework" else "TargetFrameworkVersion")
          cliArgs.Runtime, "RuntimeIdentifier"
          cliArgs.Configuration, "Configuration" ]
        |> List.choose (fun (a,p) -> a |> Option.map (fun x -> (p,x)))
        |> List.map (MSBuild.MSbuildCli.Property)

    let msbuildPath = cliArgs.MSBuild |> Option.defaultValue "msbuild"
    let dotnetPath = cliArgs.DotnetCli |> Option.defaultValue "dotnet"
    let dotnetHostPicker = cliArgs.MSBuild_Host |> Option.defaultValue MSBuildHostPicker.Auto

    let cmd = getCompilerArgsBySdk

    let rec msbuildHost host =
        match host with
        | MSBuildHostPicker.MSBuild ->
            MSBuildExePath.Path msbuildPath
        | MSBuildHostPicker.DotnetMSBuild ->
            MSBuildExePath.DotnetMsbuild dotnetPath
        | MSBuildHostPicker.Auto ->
            if isDotnetSdk then
                msbuildHost MSBuildHostPicker.DotnetMSBuild
            else
                msbuildHost MSBuildHostPicker.MSBuild
        | x ->
            failwithf "Unexpected msbuild host '%A'" x

    return projPath, getProjectInfoBySdk, cmd, (msbuildHost dotnetHostPicker), globalArgs
    }

let runCmd log workingDir exePath args =
    log (sprintf "running '%s %s'" exePath (args |> String.concat " "))

    let logOutput = System.Collections.Concurrent.ConcurrentQueue<bool*string>()

    let runProcess (workingDir: string) (exePath: string) (args: string) =
        let psi = System.Diagnostics.ProcessStartInfo()
        psi.FileName <- exePath
        psi.WorkingDirectory <- workingDir
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.Arguments <- args
        psi.CreateNoWindow <- true
        psi.UseShellExecute <- false

        //Some env var like `MSBUILD_EXE_PATH` override the msbuild used.
        //The dotnet cli (`dotnet`) set these when calling child processes, and
        //is wrong because these override some properties of the called msbuild
        let msbuildEnvVars =
            psi.Environment.Keys
            |> Seq.filter (fun s -> s.StartsWith("msbuild", StringComparison.OrdinalIgnoreCase))
            |> Seq.toList
        for msbuildEnvVar in msbuildEnvVars do
            psi.Environment.Remove(msbuildEnvVar) |> ignore


        use p = new System.Diagnostics.Process()
        p.StartInfo <- psi

        p.OutputDataReceived.Add(fun ea -> logOutput.Enqueue (false, ea.Data))

        p.ErrorDataReceived.Add(fun ea -> logOutput.Enqueue (true, ea.Data))

        p.Start() |> ignore
        p.BeginOutputReadLine()
        p.BeginErrorReadLine()
        p.WaitForExit()

        let exitCode = p.ExitCode

        exitCode

    let args = args |> String.concat " "
    let exitCode = runProcess workingDir exePath args
    let output = logOutput.ToArray()

    log "output:"
    output
        |> Seq.choose (fun (isErr, line) -> if isErr then None else Some line)
        |> Seq.iter log

    log "error:"
    output
        |> Seq.choose (fun (isErr, line) -> if isErr then Some line else None)
        |> Seq.iter log

    exitCode, (ShellCommandResult (workingDir, exePath, args, output))


/// Given a path to a .fsproj file, return the list of compiler options
let realMain projPath =  attempt {

    let cliArgs = {
        Framework = None
        Runtime = None
        Configuration = None
        MSBuild = None
        DotnetCli = None
        MSBuild_Host = None
        Verbose = false
    }

    let log =
        if cliArgs.Verbose then
            printfn "%s"
        else
            ignore

    let! (projPath, getProjectInfoBySdk, cmd, msbuildHost, globalArgs) =
        fscArgsMain log projPath cliArgs

    let globalArgs =
        match Environment.GetEnvironmentVariable("DOTNET_PROJ_INFO_MSBUILD_BL") with
        | "1" -> MSBuild.MSbuildCli.Switch("bl") :: globalArgs
        | _ -> globalArgs

    let exec getArgs additionalArgs = attempt {
        let msbuildExec =
            let projDir = Path.GetDirectoryName(projPath)
            msbuild msbuildHost (runCmd log projDir)

        let! r =
            projPath
            |> getProjectInfoBySdk log msbuildExec getArgs additionalArgs
            |> Result.mapError ExecutionError

        return r
        }

    let! r = exec cmd globalArgs

    let out =
        match r with
        | FscArgs args -> args

    out |> List.iter (printfn "%s")

    return r

    }

let wrapEx m f a =
    try
        f a
    with ex ->
        Error (RaisedException (ex, m))


let main projFile =
    match wrapEx "uncaught exception" (realMain >> runAttempt) projFile with
    | Ok _ -> 0
    | Error err ->
        match err with
        | InvalidArgsState message ->
            printfn "%s" message
            printfn "see --help for more info"
            2
        | ProjectFileNotFound projPath ->
            printfn "project file '%s' not found" projPath
            3
        | GenericError message ->
            printfn "%s" message
            4
        | RaisedException (ex, message) ->
            printfn "%s:" message
            printfn "%A" ex
            6
        | ExecutionError (MSBuildFailed (i, ShellCommandResult(wd, exePath, args, output))) ->
            printfn "msbuild exit code: %i" i
            printfn "command line was: %s> %s %s" wd exePath args
            output
                |> Seq.iter (fun (isErr, line) ->
                    if isErr then
                        printfn "stderr: %s" line
                    else printfn "stdout: %s" line)
            7
        | ExecutionError (UnexpectedMSBuildResult r) ->
            printfn "%A" r
            8
        | ExecutionError (MSBuildSkippedTarget) ->
            printfn "internal error, target was skipped"
            9
