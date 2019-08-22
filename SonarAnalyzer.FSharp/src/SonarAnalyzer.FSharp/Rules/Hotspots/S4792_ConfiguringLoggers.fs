module SonarAnalyzer.FSharp.Rules.S4792_ConfiguringLoggers

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4792 Configuring loggers is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4792
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4792";
    let messageFormat = "Make sure that this logger's configuration is safe.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
