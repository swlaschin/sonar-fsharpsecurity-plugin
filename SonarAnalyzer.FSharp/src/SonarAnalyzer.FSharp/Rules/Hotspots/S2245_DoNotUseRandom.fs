module SonarAnalyzer.FSharp.Rules.S2245_DoNotUseRandom

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #2245 Using pseudorandom number generators (PRNGs) is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2245
// =================================================


module Private =

    [<Literal>]
    let DiagnosticId = "S2245";
    let messageFormat = "Make sure that using this pseudorandom number generator is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None

