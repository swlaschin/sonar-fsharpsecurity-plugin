module SonarAnalyzer.FSharp.Rules.S2092_CookieShouldBeSecure

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open OptionBuilder
open EarlyReturn

// =================================================
// #2092 Creating cookies without the "secure" flag is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2092
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S2092"
    let messageFormat = "Make sure creating this cookie without setting the 'Secure' property is safe here."
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    // =================================
    // Common
    // =================================

    /// Helper class to store results
    type PropertyAssignmentResult = {
        UsesAllowedValue : bool
        Location : Tast.Location
        }

    /// Return one of three choices:
    /// PropertyAssignmentResult with true - if assigned using allowed value
    /// PropertyAssignmentResult with false - if assigned using other value
    /// None - if not assigned
    /// If the call is before the startingLocation it is ignored.
    let propertyAssignedWithAllowedValue className methodName allowedValue startingLine (ctx:TastContext) : PropertyAssignmentResult option =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch
            // is it a call to the given class and member?
            let! callCtx = callCtx |> CallExprHelper.tryMatchClassAndMethod className methodName
            let call = callCtx.Node
            return!
                if call.Location.StartLine >= startingLine
                && not call.Args.IsEmpty then
                    let usesAllowedValue = call.Args.[0] |> ConstHelper.isEqualTo allowedValue
                    Some {UsesAllowedValue=usesAllowedValue; Location=call.Location}
                else
                    None
            }

    let className = "HttpCookie"
    let setterMethodName = "set_Secure"
    let allowedValue = true

    let httpCookieSecureAssignedWithAllowedValue startingLine (ctx:TastContext) : PropertyAssignmentResult option =
        propertyAssignedWithAllowedValue className setterMethodName allowedValue startingLine ctx

    // =================================
    // checkSecureSetterIsUsedAfterCreation
    // =================================

    // Detect any later assignments in the body, starting at initialLocation
    // Return the last assignmentResult
    let lastAssignmentInSameScope (bodyCtx:TastContext<Tast.Expression>) (initialLocation:Tast.Location) =
        let mutable lastAssignmentResult = {UsesAllowedValue=false; Location=initialLocation}
        let accept (ctx:TastContext) =
            if ExprHelper.isConditionalExpression ctx.Node then
                false // don't drill into conditionals
            else
                httpCookieSecureAssignedWithAllowedValue initialLocation.StartLine ctx
                |> Option.iter (fun result -> lastAssignmentResult <- result)
                true // keep going

        let visitor = TastVisitor(accept)
        visitor.Visit(bodyCtx)
        lastAssignmentResult

    /// Check that if a HttpCookie is created in a member, the Secure property MUST be set to rue in the same scope
    let checkSecureSetterIsUsedAfterCreation(ctx:TastContext) =

        let newObjCtx,bodyCtx =
            option {
                // a class was created?
                let! newObjCtx = NewObjectExprHelper.tryMatch ctx
                // and it's the class we're interested in?
                let! newObjCtx = newObjCtx |> NewObjectExprHelper.tryMatchClass className
                // if so, find the body of the enclosing scope
                let! bodyCtx = newObjCtx |> ContextHelper.tryBodyOfContainingDecl

                return newObjCtx,bodyCtx
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        let newObjExpr = newObjCtx.Node

        // visit all the assignments in the body and fail if the last one does't use the correct setter
        let lastAssignmentResult =
            let initialLocation = newObjExpr.Location   // start from where the object was created
            lastAssignmentInSameScope bodyCtx initialLocation

        // the last assignment in the block determines "assignedCorrectly"
        if not lastAssignmentResult.UsesAllowedValue then
            Diagnostic.Create(rule, lastAssignmentResult.Location, className) |> Some
        else
            None

    // =================================
    // checkSecureSetterInIsolation
    // =================================

    /// Check that if Secure property is used outside of the new object creation, then it set to true
    let checkSecureSetterInIsolation (ctx:TastContext) =
        option {
            let! assignmentResult = httpCookieSecureAssignedWithAllowedValue 0 ctx
            return!
                if not assignmentResult.UsesAllowedValue then
                    Diagnostic.Create(rule, assignmentResult.Location, setterMethodName) |> Some
                else
                    None
            }


open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    let (<|>) = EarlyReturn.orElse
    let rule =
        checkSecureSetterIsUsedAfterCreation
        <|> checkSecureSetterInIsolation
    rule ctx

