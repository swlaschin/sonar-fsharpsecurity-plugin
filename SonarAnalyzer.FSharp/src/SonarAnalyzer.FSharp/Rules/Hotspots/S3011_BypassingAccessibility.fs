module SonarAnalyzer.FSharp.Rules.S3011_BypassingAccessibility

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst

// =================================================
// #3011 Changing or bypassing accessibility is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-3011
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S3011";
    let messageFormat = "Make sure that this accessibility bypass is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    /// Checks to see if the constant for BindingFlags.NonPublic is ever used.
    let checkForNonPublicFlag (ctx: TastContext) =
        let isBindingFlags (t:FSharpAst.Tast.NamedType) =
            let bindingFlagDescriptor : Tast.NamedTypeDescriptor = 
                {
                    AccessPath = "System.Reflection"
                    CompiledName = "BindingFlags"
                    DisplayName = "BindingFlags" 
                }
            t.Descriptor = bindingFlagDescriptor

        let isNonPublic (constExpr:Tast.ConstantExpr) =
            constExpr.Value = (32 |> box)

        option {
            let! call = ctx.Try<Tast.ConstantExpr>()
            match call.Type with
            | FSharpAst.Tast.NamedType t ->
                let isFailure = isBindingFlags t && isNonPublic call

                if isFailure then
                    return! Diagnostic.Create(rule, call.Location, t.Descriptor.CompiledName) |> Some
                else
                    return! None
            | _ -> return! None
            }

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    let rule =
        checkForNonPublicFlag
    rule ctx
