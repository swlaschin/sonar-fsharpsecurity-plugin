module OutputDiagnosticsDto

(*
The diagnostics are written to JSON files using

"Static Analysis Results Format (SARIF) Version 1.0.0 JSON Schema:
 a standard format for the output of static analysis and other tools."

This module contains the definitions of the JSON schema
*)

open FSharp.Data

(*
/// The Diagnostics are output in this format
type SonarJsonDiagnostics = JsonProvider<"""
{
  "$schema": "http://json.schemastore.org/sarif-1.0.0",
  "version": "1.0.0",
  "runs": [
    {
      "tool": {
        "name": "Microsoft (R) Visual F# Compiler",
        "version": "2.10.0.0",
        "fileVersion": "2.10.0.0 (45b37117)",
        "semanticVersion": "2.10.0",
        "language": "en-US"
      },
      "results": [
        {
          "ruleId": "CS0618",
          "level": "warning",
          "message": "",
          "locations": [
            {
              "resultFile": {
                "uri": "file:///C:/git_repos/mine/sonar-fsharp-security/Filename.fs",
                "region": {
                  "startLine": 41,
                  "startColumn": 46,
                  "endLine": 41,
                  "endColumn": 70
                }
              }
            }
          ],
          "relatedLocations": [
            {
              "physicalLocation": {
                "uri": "file:///C:/git_repos/mine/sonar-fsharp-security/Filename.fs",
                "region": {
                  "startLine": 71,
                  "startColumn": 29,
                  "endLine": 71,
                  "endColumn": 31
                }
              }
            }
          ],
          "properties": {
            "warningLevel": 2,
            "customProperties": {
              "0": "+3 (incl 2 for nesting)",
              "1": "+1",
              "2": "+3 (incl 2 for nesting)",
              "3": "+1",
              "4": "+3 (incl 2 for nesting)",
              "5": "+3 (incl 2 for nesting)",
              "6": "+3 (incl 2 for nesting)",
              "7": "+3 (incl 2 for nesting)",
              "8": "+3 (incl 2 for nesting)",
              "9": "+1",
              "10": "+3 (incl 2 for nesting)",
              "11": "+3 (incl 2 for nesting)"
             }
          }
        }
      ],
      "rules": {
        "CS0618": {
          "id": "CS0618",
          "shortDescription": "See separate rule definition below because records used as dictionaries dont work well",
          "fullDescription": "'CS0618' is used as a key in a dictionary",
          "defaultLevel": "warning",
          "helpUri": "https://rules.sonarsource.com/csharp/RSPEC-1135",
          "properties": {
            "category": "Compiler",
            "isEnabledByDefault": true,
            "tags": [
              "Compiler",
              "Telemetry"
            ]
          }
        },
        "S1135": {
          "id": "S3776",
          "shortDescription": "'S1135' is used as a key in a dictionary",
          "defaultLevel": "warning",
          "properties": {
            "category": "Info Code Smell",
            "isEnabledByDefault": true,
            "tags": [
              "C#"
            ]
          }
        }
      }
    }
  ]
}
""">

*)

/// Because of the way the type provider works, the format can't be generated easily,
/// so just create the types by hand
[<RequireQualifiedAccess>]
module rec JsonDiagnostics =

    open System.Collections.Generic

    type Root = {
        ``$schema`` : string
        version : string
        runs : Run[]
        }

    type Run = {
        tool: Tool
        results: Result[]
        rules : IDictionary<string,Rule>
        }

    type Tool = {
        name : string
        version : string
        fileVersion : string
        semanticVersion : string
        language : string
        }

    type Result = {
        ruleId : string
        level : string
        message : string
        locations : Location[]
        // relatedLocations // not used
        properties : ResultProperties
        }

    type Location = {
        resultFile : ResultFile
        }

    type ResultFile = {
        uri : string
        region : Region
        }

    type Region = {
        startLine: int
        startColumn: int
        endLine: int
        endColumn: int
        }

    type ResultProperties = {
        warningLevel : int
        customProperties : string []
        }

    type Rule = {
        id: string
        shortDescription: string
        fullDescription: string
        defaultLevel: string
        helpUri: string
        properties: RuleProperties
        }

    type RuleProperties = {
        category: string
        isEnabledByDefault: bool
        tags: string[]
        }
