module SonarAnalyzer.FSharp.Rules.S2255_UsingCookies

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #2255 Writing cookies is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2255
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S2255";
    let messageFormat = "Make sure that this cookie is written safely.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
