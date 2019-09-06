module FsProjectCracker.Domain

open Dotnet.ProjInfo.Inspect

type CrackerData = string list

type CrackerError =
    | NotAFsProj of projFile:string

type Errors =
    | InvalidArgsState of string
    | ProjectFileNotFound of string
    | GenericError of string
    | RaisedException of System.Exception * string
    | ExecutionError of GetProjectInfoErrors<ShellCommandResult>
and ShellCommandResult = ShellCommandResult of workingDir: string * exePath: string * args: string * output: seq<bool*string>


type MSBuildHostPicker =
    | Auto = 1
    | MSBuild  = 2
    | DotnetMSBuild = 3

/// Arguments to the CLI
type CliArgs = {
    Framework : string option
    Runtime : string option
    Configuration : string option
    MSBuild : string option
    DotnetCli : string option
    MSBuild_Host : MSBuildHostPicker option
    Verbose : bool
    }
