module SonarAnalyzer.FSharp.Rules.S3011_BypassingAccessibility

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open OptionBuilder
open EarlyReturn

// =================================================
// #3011 Changing or bypassing accessibility is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-3011
// =================================================


module Private =

    [<Literal>]
    let DiagnosticId = "S3011";
    let messageFormat = "Make sure that this accessibility bypass is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    let bindingFlagsType :Tast.NamedTypeDescriptor =
        {AccessPath="System.Reflection"; CompiledName="BindingFlags"}
    let nonPublicValue = int System.Reflection.BindingFlags.NonPublic

    /// warn if BindingFlags.NonPublic is used
    let checkAccessibility (ctx:TastContext) =
        option {
            // is it a constant?
            let! constCtx = ctx |> ContextHelper.tryCast<Tast.ConstantExpr>

            // is the type BindingFlags?
            let! namedType = TypeHelper.matchNamedType constCtx.Node.Type

            return!
                if namedType = bindingFlagsType
                && constCtx.Node.Value = box nonPublicValue then
                    Some (Diagnostic.Create(rule, constCtx.Node.Location))
                else
                    None
            }

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    checkWithEarlyReturn checkAccessibility ctx
