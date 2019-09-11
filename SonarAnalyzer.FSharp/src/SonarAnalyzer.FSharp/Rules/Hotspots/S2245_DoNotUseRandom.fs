module SonarAnalyzer.FSharp.Rules.S2245_DoNotUseRandom

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open OptionBuilder
open EarlyReturn

// =================================================
// #2245 Using pseudorandom number generators (PRNGs) is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2245
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S2245";
    let messageFormat = "Make sure that using this pseudorandom number generator is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

    let checkWithEarlyReturn f x =
        try
            f x
        with
        | :? EarlyReturn ->
            None

    let randomClass : Tast.NamedTypeDescriptor = {AccessPath="System"; CompiledName="Random"}

    let runCheck (ctx:TastContext) =
        option {
            let! ctorCtx = NewObjectExprHelper.tryMatch ctx
            let! declaringEntity = ctorCtx.Node.Ctor.DeclaringEntity

            // check for "Random" class
            if declaringEntity <> randomClass then raise EarlyReturn

            // this is in the C# code, not sure why
            let argumentsCount = ctorCtx.Node.Args.Length;
            if argumentsCount > 1 then raise EarlyReturn

            return Diagnostic.Create(rule, ctorCtx.Node.Location)
            }



open Private


/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    checkWithEarlyReturn runCheck ctx

