module SonarAnalyzer.FSharp.Rules.S4784_UsingRegularExpressions

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4784 Using regular expressions is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4784
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4784";
    let messageFormat = "Make sure that using a regular expression is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
