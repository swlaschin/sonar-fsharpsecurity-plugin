module SonarAnalyzer.FSharp.Rules.S4790_CreatingHashAlgorithms

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4790 Hashing data is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4790
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4790";
    let messageFormat = "Make sure that hashing data is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
