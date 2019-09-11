module SonarAnalyzer.FSharp.Rules.S4507_DeliveringDebugFeaturesInProduction

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open OptionBuilder
open EarlyReturn

// =================================================
// #4507 Delivering code in production with debug features activated is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4507
// =================================================


module Private =

    [<Literal>]
    let DiagnosticId = "S4507";
    let messageFormat = "Make sure this debug feature is deactivated before delivering the code in production.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    /// Does the ErrorPage context live under an "if" where the if expression is
    /// HostingEnvironmentExtensions.IsDevelopment or IHostingEnvironment.IsDevelopment()
    let isInDevelopmentCheck (errorPageCallCtx:TastContext) =

        let isIfThenElse (node:obj) =
            match box node with
            | :? Tast.IfThenElseExpr -> true
            | _ -> false

        let mutable foundIsDevelopment = false
        let mutable foundOnTrueBranch = false

        // drill down into the condition, looking for a call to "IsDevelopment"
        let acceptCondition (ctx:TastContext) =
            option {
                // see if it is a call to IsDevelopment
                let! callCtx = ctx |> CallExprHelper.tryMatch

                ["IsDevelopment"; "get_IsDevelopment"]
                |> List.contains callCtx.Node.Member.CompiledName
                |> fun _ -> foundIsDevelopment <- true

                // keep visiting
                return true
                }
            |> Option.defaultValue true // keep going

        // drill down into the IfTrue branch, looking for the errorPageCallCtx
        let acceptIfTrue (ctx:TastContext) =
            if errorPageCallCtx = ctx then foundOnTrueBranch <- true
            true

        option {
            let! ifThenElseExprCtx = errorPageCallCtx |> ContextHelper.tryCastAncestor<Tast.IfThenElseExpr>
            let conditionCtx = ifThenElseExprCtx.Push(ifThenElseExprCtx.Node.Condition)
            let conditionVisitor = TastVisitor(acceptCondition)
            conditionVisitor.Visit(conditionCtx)

            if foundIsDevelopment then
                // check that we're on the true branch
                let ifTrueCtx = ifThenElseExprCtx.Push(ifThenElseExprCtx.Node.IfTrue)
                let ifTrueVisitor = TastVisitor(acceptIfTrue)
                ifTrueVisitor.Visit(ifTrueCtx)

            return foundIsDevelopment && foundOnTrueBranch
            }
        |> Option.defaultValue false

    let checkDebugFeatures (ctx:TastContext) =
        option {
            let! callCtx = CallExprHelper.tryMatch ctx

            // check for certain methods
            if ["UseDeveloperExceptionPage"; "UseDatabaseErrorPage"]
               |> List.contains callCtx.Node.Member.CompiledName
               |> not then raise EarlyReturn

            // if used in DevelopmentCheck condition only
            if isInDevelopmentCheck ctx then raise EarlyReturn

            return Diagnostic.Create(rule, callCtx.Node.Location)
        }


open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    checkWithEarlyReturn checkDebugFeatures ctx
