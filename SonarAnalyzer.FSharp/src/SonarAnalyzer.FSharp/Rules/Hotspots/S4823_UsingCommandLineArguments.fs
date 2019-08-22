module SonarAnalyzer.FSharp.Rules.S4823_UsingCommandLineArguments

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4823 Using command line arguments is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4823
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4823";
    let messageFormat = "Make sure that command line arguments are used safely here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
