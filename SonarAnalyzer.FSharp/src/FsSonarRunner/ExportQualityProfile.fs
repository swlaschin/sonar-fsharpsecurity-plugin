module ExportQualityProfile

(*
A QualityProfile file contains, for each rule, whether it is active, what it's severity is, etc.
See https://docs.sonarqube.org/latest/instance-administration/quality-profiles/

The default (non-overridable) profile is called "Sonar Way" and is provided by the plugin.

For this plugin, they will all be active by default, and the severity is default too.

So all will do is write a list of rule keys, one per line.

NOTE: There is no standard format for this file: it is a contract between this exporter and the corresponding importer
at .\sonar-fsharpsecurity-plugin\src\main\java\org\sonar\plugins\fsharp\FSharpSonarWayProfile.java

*)

open System
open System.IO
open SonarAnalyzer.FSharp

let profileFile = "profile.txt"

let logger = Serilog.Log.Logger

// ensure the directory exists
let ensure dirname = Directory.CreateDirectory(dirname) |> ignore; dirname

/// Write all available rules to the profile file
let write(dirname) =

    let dirname = dirname |> ensure
    let profilePath = IO.Path.Combine(dirname, profileFile)

    logger.Information("Writing quality profile file to {profilePath}",profilePath)

    let rules = RuleManager.getAvailableRules()
    let ruleKeys = rules |> List.map (fun r -> r.RuleId)

    File.WriteAllLines(profilePath, ruleKeys)
