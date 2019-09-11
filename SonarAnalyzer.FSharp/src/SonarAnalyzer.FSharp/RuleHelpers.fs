module SonarAnalyzer.FSharp.RuleHelpers

open FSharpAst
open OptionBuilder

// =====================================
// Helper functions for rules
// =====================================

module ContextHelper =

    /// Convert the TastContext<_> into a TastContext<obj>
    let boxContext (ctx:TastContext<'T>) :TastContext =
        ctx.Box

     /// Return the ctx if the node matches the specified type
    let tryCast<'T> (ctx:TastContext) :TastContext<'T> option =
        ctx.TryCast<'T>()

    /// return the context for the first ancestor or self that matches the condition
    let tryAncestorOrSelf condition (ctx:TastContext<'T>) =
        let rec loop (ctx:TastContext) =
            if condition ctx.Node then
                Some ctx
            else
                ctx.TryPop()
                |> Option.bind loop
        loop ctx.Box

    /// return the context for the first ancestor that matches the condition
    let tryAncestor condition (ctx:TastContext<'T>) =
        ctx.TryPop()
        |> Option.bind (tryAncestorOrSelf condition)

    /// return the context for the first ancestor or self that matches the type parameter
    let tryCastAncestorOrSelf<'T> (ctx:TastContext<_>) :TastContext<'T> option =
        let condition node =
            match box node with
            | :? 'T -> true
            | _ -> false
        ctx
        |> tryAncestorOrSelf condition
        |> Option.bind (tryCast<'T>)

    /// return the context for the first ancestor that matches the type parameter
    let tryCastAncestor<'T> (ctx:TastContext)  =
        ctx.TryPop()
        |> Option.bind (tryCastAncestorOrSelf<'T>)

    /// Find the body of the MFV declaration that this node is contained in
    let tryBodyOfContainingDecl (ctx:TastContext<_>) : TastContext<Tast.Expression> option =

        /// Call the first function and if that fails, call the second function
        let ( <|> ) f g = fun x -> match f x with | Some r -> Some r | None -> g x

        let getMemberBody() =
            ctx.Box
            |> tryCastAncestorOrSelf<Tast.MemberDecl>
            |> Option.map (fun ctx -> ctx.Push(ctx.Node.Body))

        let getFunctionBody() =
            ctx.Box
            |> tryCastAncestorOrSelf<Tast.FunctionDecl>
            |> Option.map (fun ctx -> ctx.Push(ctx.Node.Body))

        let getTopLevelLambdaBody() =
            ctx.Box
            |> tryCastAncestorOrSelf<Tast.TopLevelLambdaValueDecl>
            |> Option.map (fun ctx -> ctx.Push(ctx.Node.Body))

        (getMemberBody <|> getFunctionBody <|> getTopLevelLambdaBody)()

/// helpers specific to expressions
module ExprHelper =
    open ContextHelper

    /// Find the Expression that this node is contained in
    let tryContainingExpr (ctx:TastContext) =
        ctx |> tryCastAncestorOrSelf<Tast.Expression>

    let isConditionalExpression (node:obj) =
        match box node with
        | :? Tast.IfThenElseExpr -> true
        | :? Tast.UnionCaseTestExpr -> true
        | _ -> false


/// helpers specific to call expressions
module CallExprHelper =
    open ContextHelper

    /// Match the context if it is a call expression
    let tryMatch (ctx:TastContext) : TastContext<Tast.CallExpr> option =
        ctx |> tryCast<Tast.CallExpr>

    /// Match a CallExpr context if it has the given method name
    let tryMatchMethod methodName (ctx:TastContext<Tast.CallExpr>) : TastContext<Tast.CallExpr> option =
        if ctx.Node.Member.CompiledName = methodName then Some ctx else None

    /// Match a CallExpr context if it has the given class name
    let tryMatchClass className (ctx:TastContext<Tast.CallExpr>) : TastContext<Tast.CallExpr> option =
        ctx.Node.Member.DeclaringEntity
        |> Option.bind (fun e -> if e.CompiledName = className then Some ctx else None)

    /// Match a CallExpr context if it has the given class and method name
    let tryMatchClassAndMethod className methodName (ctx:TastContext<Tast.CallExpr>) : TastContext<Tast.CallExpr> option =
        ctx
        |> (tryMatchClass className)
        |> Option.bind (tryMatchMethod methodName)

    /// Match a CallExpr context if it matches the Mfv descriptor
    let tryMatchMfv (desc: Tast.MfvDescriptor) (ctx:TastContext<Tast.CallExpr>) =
        if ctx.Node.Member = desc then Some ctx else None

    /// Find the Call that this node is contained in
    let tryContainingCall ctx =
        ctx |> tryCastAncestorOrSelf<Tast.CallExpr>

    /// Detect if this context is used in an argument to a CallExpr.
    /// If so, return the index of the argument or None if not found
    let tryGetArgumentAt index (ctx:TastContext<Tast.CallExpr>) : Tast.Expression option =
        if ctx.Node.Args.Length <= index then
            None
        else
            Some ctx.Node.Args.[index]

    /// Detect if this context is used in an argument to a CallExpr.
    /// If so, return the index of the argument or None if not found
    let tryArgumentIndex (child:TastContext) : int option =

        // Return true if the context is a child of an argument expression
        let isChildOfArg (callExprContext:TastContext<Tast.CallExpr>) (argExpr:Tast.Expression) =
            let argExprContext = callExprContext.Push(argExpr)
            let mutable found = false
            let accept argChild =
                found <- argChild.Node = child.Node
                true // keep going
            let v = TastVisitor(accept)
            v.Visit(argExprContext)
            found

        option {
            // get the call the child node belongs to
            let! callCtx = tryContainingCall child
            // loop through the arguments, trying to find a match
            return!
                callCtx.Node.Args
                |> List.tryFindIndex (isChildOfArg callCtx)
            }

    /// Detect if this context is used in an argument to a CallExpr.
    /// If so, return true
    let isInArgument (ctx:TastContext<_>) =
        (tryArgumentIndex ctx).IsSome


/// helpers specific to NewObject expressions
module NewObjectExprHelper =
    open ContextHelper

    /// Match the context if it is a NewObject expression
    let tryMatch ctx =
        ctx |> tryCast<Tast.NewObjectExpr>

    /// Match a NewObject context if it has the given class name
    let tryMatchClass className (ctx:TastContext<Tast.NewObjectExpr>) :TastContext<Tast.NewObjectExpr> option =
        ctx.Node.Ctor.DeclaringEntity
        |> Option.bind (fun e -> if e.CompiledName = className then Some ctx else None)

    /// Match the context if it is a NewObject expression and matches the named type descriptor
    let tryMatchNamedType (desc: Tast.NamedTypeDescriptor) (ctx:TastContext<Tast.NewObjectExpr>) :TastContext<Tast.NewObjectExpr> option =
        if ctx.Node.Ctor.DeclaringEntity = Some desc then Some ctx else None

    /// Find the NewObjectExpr that this node is contained in
    let tryContainingNewObjectExpr (ctx:TastContext<_>) =
        ctx |> tryCastAncestorOrSelf<Tast.NewObjectExpr>

module ConstHelper =

    /// check whether the values are the same, independent of location
    let areSame (expr1:Tast.Expression) (expr2:Tast.Expression) =
        match expr1,expr2 with
        | Tast.ConstantExpr expr1,Tast.ConstantExpr expr2 ->
            expr1.Value = expr2.Value
        | _ ->
            false

    /// check whether the constant expression has the provided value
    let isEqualTo value (expr:Tast.Expression) =
        match expr with
        | Tast.ConstantExpr expr ->
            expr.Value = box value
        | _ ->
            false

module EntityHelper =
    open FSharpAst

    let tryGet (desc:Tast.NamedTypeDescriptor) : Tast.Entity option =
        failwith "not implemented"

    let implementedInterfaces (entity:Tast.Entity) : Tast.NamedTypeDescriptor list =
        failwith "not implemented"

    let implements interfaceName (entity:Tast.Entity) : bool =
        entity
        |> implementedInterfaces
        |> List.exists (fun i -> i.CompiledName = interfaceName)

    let baseClasses (entity:Tast.Entity) : Tast.NamedTypeDescriptor list =
        failwith "not implemented"

    let derives baseClassName (entity:Tast.Entity) : bool =
        entity
        |> baseClasses
        |> List.exists (fun i -> i.CompiledName = baseClassName )


module TypeHelper =
    open FSharpAst

    /// Is it a named type?
    /// Call with "myType |> isType KnownType.string"
    let matchNamedType (tastType:Tast.FSharpType) : Tast.NamedTypeDescriptor option =
        match tastType with
        | Tast.NamedType namedType ->
            Some namedType.Descriptor
        | Tast.UnknownFSharpType _
        | Tast.VariableType _
        | Tast.TupleType _
        | Tast.FunctionType _
        | Tast.ArrayType _
        | Tast.OtherType _
        | Tast.ByRefType _ ->
            None

    /// Call with "myType |> isType KnownType.string"
    let isType (desc:Tast.NamedTypeDescriptor) (tastType:Tast.FSharpType) :bool =
        tastType
        |> matchNamedType
        |> Option.map (fun d -> d = desc)
        |> Option.defaultValue false



let nullLocation = Tast.Location.NullLocation

///// Create a ConstantExpression to test against
//let createConstantExpression constValue constType : Tast.Expression =
//    Tast.ConstantExpr {
//        Type = Tast.NamedType {Descriptor = constType; TypeArgs = [] }
//        Value = constValue
//        Location = nullLocation
//        }

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
