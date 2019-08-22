module SonarAnalyzer.FSharp.Rules.S4818_SocketsCreation

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4818 Using Sockets is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4818
// =================================================


module Private =

    [<Literal>]
    let DiagnosticId = "S4818";
    let messageFormat = "Make sure that sockets are used safely here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    None
