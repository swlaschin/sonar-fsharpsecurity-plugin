module SonarAnalyzer.FSharp.Rules.S4834_ControllingPermissions

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4834 Controlling permissions is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4834
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4834";
    let messageFormat = "Make sure that permissions are controlled safely here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
