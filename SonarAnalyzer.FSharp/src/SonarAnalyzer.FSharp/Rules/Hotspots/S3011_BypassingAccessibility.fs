module SonarAnalyzer.FSharp.Rules.S3011_BypassingAccessibility

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #3011 Changing or bypassing accessibility is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-3011
// =================================================


module Private =

    [<Literal>]
    let DiagnosticId = "S3011";
    let messageFormat = "Make sure that this accessibility bypass is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
