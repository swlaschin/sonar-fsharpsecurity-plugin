module SonarAnalyzer.FSharp.Rules.S4507_DeliveringDebugFeaturesInProduction

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4507 Delivering code in production with debug features activated is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4507
// =================================================


module Private =

    [<Literal>]
    let DiagnosticId = "S4507";
    let messageFormat = "Make sure this debug feature is deactivated before delivering the code in production.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
