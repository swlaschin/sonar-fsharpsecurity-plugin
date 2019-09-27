module SonarAnalyzer.FSharp.Rules.S2245_DoNotUseRandom

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst

// =================================================
// #2245 Using pseudorandom number generators (PRNGs) is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2245
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S2245";
    let messageFormat = "Make sure that using this pseudorandom number generator is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    /// Checks to see if the ctor for System.Random is ever called.
    let checkForRandomConstructor (ctx: TastContext) =
        let ctorForRandom : Tast.MemberDescriptor = 
            {
                DeclaringEntity = Some { AccessPath = "System"; DisplayName = "Random"; CompiledName = "Random" }
                CompiledName = ".ctor"
                DisplayName = ".ctor" 
            }

        option {
            let! call = ctx.Try<Tast.NewObjectExpr>()
            if call.Ctor = ctorForRandom then
                return! Diagnostic.Create(rule, call.Location, call.Ctor.CompiledName) |> Some
            else
                return! None
            }

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    let rule =
        checkForRandomConstructor
    rule ctx

