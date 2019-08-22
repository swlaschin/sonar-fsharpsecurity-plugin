module OutputDiagnostics

(*
The diagnostics are written to JSON files.

This module contains the logic for writing them.
*)

open System
open System.IO
open SonarAnalyzer.FSharp
open OutputDiagnosticsDto
open Newtonsoft.Json

let logger = Serilog.Log.Logger

let fileToUri filename =
    let f = System.IO.FileInfo(filename)
    match Uri.TryCreate(f.FullName,UriKind.Absolute) with
    | true, uri -> uri.AbsoluteUri
    | false, _ -> ""

// the severity string must be in the list from the schema "http://json.schemastore.org/sarif-1.0.0"
let toJsonSeverity (severity:DiagnosticSeverity) =
    match severity with
    | DiagnosticSeverity.Warning -> "warning"
    | DiagnosticSeverity.Error -> "error"
    | DiagnosticSeverity.Info -> "note"
    | DiagnosticSeverity.Hidden -> "pass"
    | _ -> "pass"


let dtoFromDiagnostic (diagnostic:Diagnostic) =

    // log it
    logger.Warning("{filename}({row},{col}): {Severity} {RuleId}: {message}",
        diagnostic.Location.FileName,
        diagnostic.Location.StartLine,
        diagnostic.Location.StartColumn,
        diagnostic.Severity,
        diagnostic.Descriptor.Id,
        diagnostic.Message
        )

    let result : JsonDiagnostics.Result = {
        ruleId = diagnostic.Descriptor.Id
        level = diagnostic.Descriptor.DefaultSeverity |> toJsonSeverity
        message = diagnostic.Message
        locations = [|
            {
                resultFile = {
                    uri = diagnostic.Location.FileName |> fileToUri
                    region = {
                        startLine = diagnostic.Location.StartLine
                        startColumn = diagnostic.Location.StartColumn
                        endLine = diagnostic.Location.EndLine
                        endColumn = diagnostic.Location.EndColumn
                        }
                    }
            }|]
        //relatedLocations = [| |]
        properties = {
            warningLevel = 1
            customProperties = [||]
            }
        }


    let rule : JsonDiagnostics.Rule = {
        id = diagnostic.Descriptor.Id
        shortDescription = diagnostic.Descriptor.Title
        fullDescription = diagnostic.Descriptor.Description
        defaultLevel = diagnostic.Descriptor.DefaultSeverity |> toJsonSeverity
        helpUri = diagnostic.Descriptor.HelpLinkUri
        properties = {
            category = diagnostic.Descriptor.Category
            isEnabledByDefault = diagnostic.Descriptor.IsEnabledByDefault
            tags = diagnostic.Descriptor.CustomTags |> List.toArray
            }
        }

    result, rule

// create
let dtoFromDiagnosticList (diagnostics:Diagnostic list) =

    let results, rules =
        diagnostics
        |> List.map dtoFromDiagnostic
        |> List.unzip

    let rulesDict =
        rules
        |> List.map ( fun r -> r.id, r)
        |> dict

    // Copied from https://github.com/fsharp/FSharp.Compiler.Service/blob/master/fcs/FSharp.Compiler.Service/AssemblyInfo.fs
    // TODO should this be fetched from the FSC dll? Does it matter?
    let tool : JsonDiagnostics.Tool = {
        name = "FSharp.Compiler.Service"
        version = "4.4.1.0"
        fileVersion = "2017.06.27.0"
        semanticVersion = "4.4.1.0"
        language = "en-US"
        }

    let run : JsonDiagnostics.Run = {
        tool = tool
        results = results |> List.toArray
        rules = rulesDict
        }

    let dto : JsonDiagnostics.Root = {
        ``$schema`` = "http://json.schemastore.org/sarif-1.0.0"
        version = "1.0.0"
        runs = [| run |]
    }

    dto

/// Write the diagnostics to the output file or stdout
let outputTo (outputFilename:string) (diagnostics:Diagnostic list) =

    let diagnosticsDto = dtoFromDiagnosticList diagnostics


    // Use stream writer to help with memory
    let serializer = new JsonSerializer();
    use sw = new IO.StreamWriter(outputFilename)
    use jsonWriter = new JsonTextWriter(sw)
    jsonWriter.Formatting <- Formatting.Indented;
    serializer.Serialize(jsonWriter, diagnosticsDto)
