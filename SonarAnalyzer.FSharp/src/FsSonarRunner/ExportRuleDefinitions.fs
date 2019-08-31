module ExportRuleDefinitions

(*
A RuleDefinitions file contains all the rule details in XML format

This module contains logic to extract the available rules and export them as a file.
*)

open SonarAnalyzer.FSharp
open RuleDefinitionDto
open System
open System.IO


let ruleFile = "rules.xml"
let lang = "fsharp"

let logger = Serilog.Log.Logger

// ensure the directory exists
let ensure dirname = Directory.CreateDirectory(dirname) |> ignore; dirname

/// Write all available rules to the rules file
let write(dirname) =

    let dirname = dirname |> ensure
    let rulePath = IO.Path.Combine(dirname, ruleFile)

    logger.Information("Writing rule definitions file to {rulePath}",rulePath)

    let rules = RuleManager.getAvailableRules()
    let root = RuleDefinitionsDto.toDto rules
    Utilities.serializeToXmlFile rulePath root
