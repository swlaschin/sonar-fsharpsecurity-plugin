module SonarAnalyzer.FSharp.Rules.S4829_ReadingStandardInput

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4829 Reading the Standard Input is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4829
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4829";
    let messageFormat = "Make sure that reading the standard input is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
