module SonarAnalyzer.FSharp.RuleHelpers

open SonarAnalyzer.FSharp
open FSharpAst
open System

// =====================================
// Helper functions for rules
// =====================================

type OptionBuilder() =
    member __.Return(x) = Some x
    member __.ReturnFrom(x) = x
    member __.Bind(x,f) = x |> Option.bind f
    member __.Zero() = Some ()
    member __.Combine(m, f) = Option.bind f m
    member __.Delay(f: unit -> _) = f
    member __.Run(f) = f()
    member this.TryWith(m, h) =
        try this.ReturnFrom(m)
        with e -> h e

    member this.TryFinally(m, compensation) =
        try this.ReturnFrom(m)
        finally compensation()

    member this.Using(res:#IDisposable, body) =
        this.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

    member this.While(guard, f) =
        if not (guard()) then Some () else
        do f() |> ignore
        this.While(guard, f)

    member this.For(sequence:seq<_>, body) =
        this.Using(sequence.GetEnumerator(),
            fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> body enum.Current)))

let option = OptionBuilder()

/// Does the node's type match the expected type?
/// Call with "myType |> isType KnownType.string"
let isType typeDesc (tastType:Tast.FSharpType) =
    match tastType with
    | Tast.NamedType namedType ->
        namedType.Descriptor = typeDesc
    | Tast.UnknownFSharpType _
    | Tast.VariableType _
    | Tast.TupleType _
    | Tast.FunctionType _
    | Tast.ArrayType _
    | Tast.OtherType _
    | Tast.ByRefType _ -> false

/// Find the Expression that this node is contained in
let tryContainingExpr (context:TastContext) =
    context.TryAncestorOrSelf<Tast.Expression>()

/// Find the Call that this node is contained in
let tryContainingCall (context:TastContext) =
    context.TryAncestorOrSelf<Tast.CallExpr>()

/// Find the NewObjectExpr that this node is contained in
let tryContainingNewObjectExpr (context:TastContext) =
    context.TryAncestorOrSelf<Tast.NewObjectExpr>()

/// Find the body of the Member/Function/TopLevelLambda declaration that this node is contained in
let tryBodyOfContainingDecl (context:TastContext) =

    /// Call the first function and if that fails, call the second function
    let ( <|> ) f g = fun x -> match f x with | Some r -> Some r | None -> g x

    let getMemberBody() =
        context.TryAncestorOrSelf<Tast.MemberDecl>()
        |> Option.map (fun (ctx,node) -> ctx.Push(node.Body), node.Body)

    let getFunctionBody() =
        context.TryAncestorOrSelf<Tast.FunctionDecl>()
        |> Option.map (fun (ctx,node) -> ctx.Push(node.Body), node.Body)

    let getTopLevelLambdaBody() =
        context.TryAncestorOrSelf<Tast.TopLevelLambdaValue>()
        |> Option.map (fun (ctx,node) -> ctx.Push(node.Body), node.Body)

    (getMemberBody <|> getFunctionBody <|> getTopLevelLambdaBody)()


// Detect if this context is used in an argument to a CallExpr.
// If so, return the index of the argument or None if not found
let tryArgumentIndex (child:TastContext) =

    // Return true if the context is a child of an argument expression
    let isChildOfArg (callExprContext:TastContext) (argExpr:Tast.Expression) =
        let argExprContext = callExprContext.Push(argExpr)
        let mutable found = false
        let accept argChild =
            found <- argChild.Node = child.Node
            true // keep going
        let v = TastVisitor(accept)
        v.Visit(argExpr,argExprContext)
        found

    option {
        // get the call the child node belongs to
        let! callCtx,callExp = tryContainingCall child
        // loop through the arguments, trying to find a match
        return!
            callExp.Args
            |> List.tryFindIndex (isChildOfArg callCtx)
        }

// Detect if this context is used in an argument to a CallExpr.
// If so, return true
let isInArgument (context:TastContext) =
    (tryArgumentIndex context).IsSome

let nullLocation = Tast.Location.NullLocation

/// Create a ConstantExpression to test against
let createConstantExpression constValue constType : Tast.Expression =
    Tast.ConstantExpr {
        Type = Tast.NamedType {Descriptor = constType; TypeArgs = [] }
        Value = constValue
        Location = nullLocation
        }

module ObjectCreation =
    ()
    //let objectCreatedWithAllowedValue (node:Tast.NewObjectExpr) =
    //            !ObjectCreatedWithAllowedValue(objectCreation, c.SemanticModel) &&
    //            !IsLaterAssignedWithAllowedValue(objectCreation, c.SemanticModel))
    //        {
    //            c.ReportDiagnosticWhenActive(Diagnostic.Create(SupportedDiagnostics[0], objectCreation.GetLocation()));
    //        }
    //    },
    //    SyntaxKind.ObjectCreationExpression);

    //                ccc.RegisterSyntaxNodeActionInNonGenerated(
    //                    c =>
    //                    {
    //                        var assignment = (AssignmentExpressionSyntax)c.Node;

    //                        // Ignore assignments within object initializers, they are
    //                        // reported in the ObjectCreationExpression handler
    //                        if (assignment.FirstAncestorOrSelf<InitializerExpressionSyntax>() == null &&
    //                            IsTrackedPropertyName(assignment.Left) &&
    //                            IsPropertyOnTrackedType(assignment.Left, c.SemanticModel) &&
    //                            !IsAllowedValue(assignment.Right, c.SemanticModel))
    //                        {
    //                            c.ReportDiagnosticWhenActive(Diagnostic.Create(SupportedDiagnostics[0], assignment.GetLocation()));
    //                        }
    //                    },
    //                    SyntaxKind.SimpleAssignmentExpression);
    //            });
