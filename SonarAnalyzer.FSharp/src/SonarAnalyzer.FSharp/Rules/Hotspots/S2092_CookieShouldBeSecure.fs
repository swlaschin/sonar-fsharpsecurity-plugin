module SonarAnalyzer.FSharp.Rules.S2092_CookieShouldBeSecure

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst

// =================================================
// #2092 Creating cookies without the "secure" flag is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2092
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S2092"
    let messageFormat = "Make sure creating this cookie without setting the 'Secure' property is safe here."
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

    let checkWithEarlyReturn f x =
        try
            f x
        with
        | :? EarlyReturn ->
            None

    // =================================
    // Common
    // =================================

    let isSameConst (expr1:Tast.Expression) (expr2:Tast.Expression) =
        match expr1,expr2 with
        | Tast.ConstantExpr expr1,Tast.ConstantExpr expr2 ->
            expr1.Value = expr2.Value
        | _ ->
            false

    /// If the context is a CallExpr that matches the className and methodName, return it.
    /// Else return None if no match.
    let tryGetCallExpr className methodName (ctx:TastContext) =
        option {
            let! call = ctx.Try<Tast.CallExpr>()
            let! declaringEntity = call.Member.DeclaringEntity

            return!
                if className = declaringEntity.CompiledName
                    && call.Member.CompiledName = methodName then
                    Some call
                else
                    None
            }

    /// Return one of three choices:
    /// Some true,location - if assigned correctly
    /// Some false,location - if assigned incorrectly
    /// None - if not assigned or not applicable
    /// If the call is before the startingLocation it is ignored.
    let propertyAssignedWithAllowedValue className methodName allowedValue startingLine (ctx:TastContext) =
        option {
            // this will be a call to "set_PropertyName"
            let! call = tryGetCallExpr className methodName ctx

            return!
                if call.Location.StartLine >= startingLine
                    && not call.Args.IsEmpty then
                    Some (isSameConst call.Args.[0] allowedValue, call.Location)
                else
                    None
            }

    let className = "HttpCookie"
    let setterMethodName = "set_Secure"
    let allowedValue = createConstantExpression true WellKnownType.bool

    let tryGetHttpCookieSecureAssignment =
        tryGetCallExpr className setterMethodName

    let httpCookieSecureAssignedWithAllowedValue =
        propertyAssignedWithAllowedValue className setterMethodName allowedValue

    // =================================
    // checkSecureSetterIsUsedAfterCreation
    // =================================

    // Detect an assignment in the body, starting at initialLocation
    // and check whether it was assigned correctly.
    // Return assignedCorrectly (true/false) and the location
    let detectAssignmentInSameScope (bodyExpr:Tast.Expression) bodyCtx (initialLocation:Tast.Location) =
        let mutable assignedCorrectly = false
        let mutable errorLocation = initialLocation
        let accept (ctx:TastContext) =
            if ctx.Try<Tast.IfThenElseExpr>().IsSome then
                false // don't drill into conditionals
            elif ctx.Try<Tast.UnionCaseTestExpr>().IsSome then
                false // don't drill into conditionals
            else
                httpCookieSecureAssignedWithAllowedValue initialLocation.StartLine ctx
                |> Option.iter (fun (b,l) -> assignedCorrectly <- b; errorLocation <- l)
                true // keep going
        let visitor = TastVisitor(accept)
        visitor.Visit(bodyExpr,bodyCtx)

        assignedCorrectly,errorLocation

    /// Check that if a HttpCookie is created in a member, the Secure property MUST be set to rue in the same scope
    let checkSecureSetterIsUsedAfterCreation(ctx:TastContext) =

        let newObjExpr, declaringEntity =
            option {
                let! newObjExpr = ctx.Try<Tast.NewObjectExpr>()
                let! declaringEntity = newObjExpr.Ctor.DeclaringEntity
                return newObjExpr,declaringEntity
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // is the class the one we're interested in?
        if not (className = declaringEntity.CompiledName) then raise EarlyReturn

        // if so, find the body of the enclosing scope
        let bodyCtx, bodyExpr =
            tryBodyOfContainingDecl ctx
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // visit all the assignments in the body and fail if none of them use the correct setter
        let assignedCorrectly,errorLocation =
            let initialLocation = newObjExpr.Location   // start from where the object was created
            detectAssignmentInSameScope bodyExpr bodyCtx initialLocation

        // the last assignment in the block determines "assignedCorrectly"
        if not assignedCorrectly then
            Diagnostic.Create(rule, errorLocation, className) |> Some
        else
            None

    // =================================
    // checkSecureSetterInIsolation
    // =================================

    /// Check that if Secure property is used outside of the new object creation, then it set to true
    let checkSecureSetterInIsolation (ctx:TastContext) =

        httpCookieSecureAssignedWithAllowedValue 0 ctx
        |> Option.bind (fun (assignedCorrectly, location) ->
            if not assignedCorrectly then
                Diagnostic.Create(rule, location, setterMethodName) |> Some
            else
                None
            )


    /// Call the first function and if that fails, call the second function
    let ( <|> ) f g x =
        match (f x) with
        | Some r -> Some r
        | None -> g x

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    let rule =
        (checkWithEarlyReturn checkSecureSetterIsUsedAfterCreation)
        <|> (checkWithEarlyReturn checkSecureSetterInIsolation)
    rule ctx

