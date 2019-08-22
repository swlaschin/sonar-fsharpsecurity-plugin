module SonarAnalyzer.FSharp.Rules.S5042_ExpandingArchiveFiles

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #5042 Expanding archive files is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-5042
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S5042";
    let messageFormat = "Make sure that decompressing this archive file is safe.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
