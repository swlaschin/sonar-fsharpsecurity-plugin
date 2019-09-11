module OutputDiagnostics

(*
The diagnostics are written to JSON files.

This module contains the logic for writing them.
*)

open SonarAnalyzer.FSharp
open Newtonsoft.Json

let logger = Serilog.Log.Logger
let loggerPrefix = "FsSonarRunner"

/// Write the diagnostics to the output file
let outputTo (xmlFilename:string) (diagnostics:Diagnostic list) =

    logger.Information("[{prefix}] Writing diagnostics to {filename}", loggerPrefix,xmlFilename)
    let dto = diagnostics |> DiagnosticsDto.toDto
    Utilities.serializeToXmlFile xmlFilename dto

