namespace FSharpAst


/// An exception can be thrown to stop iterating
exception StopIteration

type TastContext = {
    Filename : string
    Node : obj
    Ancestors : obj list
    }
    with

    // Push a new child node into the context.
    member this.Push(childNode) =
        let newAncestors = this.Node :: this.Ancestors
        {this with Node=childNode; Ancestors=newAncestors}

    // Pop the context
    member this.TryPop() =
        match this.Ancestors with
        | [] ->
            None
        | head::tail ->
            Some {this with Node=head; Ancestors=tail}

    // return the node if it matches the type parameter
    member this.Try<'T>() =
        match this.Node with
        | :? 'T as node -> Some node
        | _ -> None

    // return the context for the first ancestor or self that matches the condition
    member this.TryAncestorOrSelfCtx(condition) =
        if condition this.Node then
            Some this
        else
            this.TryPop()
            |> Option.bind (fun ctx -> ctx.TryAncestorOrSelfCtx(condition))

    // return the context for the first ancestor that matches the condition
    member this.TryAncestorCtx(condition) =
        this.TryPop()
        |> Option.bind (fun ctx -> ctx.TryAncestorOrSelfCtx(condition))

    // return the context and node for the first ancestor or self that matches the type parameter
    member this.TryAncestorOrSelf<'T>() =
        let condition (node:obj) =
            match node with
            | :? 'T -> true
            | _ -> false
        this.TryAncestorOrSelfCtx(condition)
        |> Option.map (fun ctx -> ctx, ctx.Node :?> 'T)

    // return the context and node for the first ancestor that matches the type parameter
    member this.TryAncestor<'T>() =
        this.TryPop()
        |> Option.bind (fun ctx -> ctx.TryAncestorOrSelf<'T>())

// For each node in the tree, run the "accept" function. If it returns true, keep going, else stop.
type TastVisitor(accept: TastContext -> bool) =

    member this.Visit(node:Tast.ImplementationFile) =
        let context = {Filename=node.Name; Node=node; Ancestors=[]}
        for child in node.Decls do
            this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ImplementationFileDecl, context:TastContext) =
        if accept context then
            match node with
            | Tast.Entity child -> this.Visit(child,context.Push(child))
            | Tast.MemberOrFunctionOrValue child -> this.Visit(child,context.Push(child))
            | Tast.InitAction child -> this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.Entity, context:TastContext) =
        if accept context then
            match node with
            | Tast.Namespace child -> this.Visit(child,context.Push(child))
            | Tast.Module child -> this.Visit(child,context.Push(child))
            | Tast.TypeEntity child -> this.Visit(child,context.Push(child))
            | Tast.UnhandledEntity _ -> ()

    member this.Visit(node:Tast.Namespace, context:TastContext) =
        if accept context then
            for child in node.SubDecls do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.Module, context:TastContext) =
        if accept context then
            for child in node.SubDecls do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.TypeEntity, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.MemberOrFunctionOrValue, context:TastContext) =
        if accept context then
            match node with
            | Tast.Member child -> this.Visit(child,context.Push(child))
            | Tast.Function child -> this.Visit(child,context.Push(child))
            | Tast.Value child -> this.Visit(child,context.Push(child))
            | Tast.UnhandledMemberOrFunctionOrValue _ -> ()
            | Tast.TopLevelLambdaValue child -> this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.InitAction, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.MemberDecl, context:TastContext) =
        try
            if accept context then
                for arg in node.Parameters do
                    this.Visit(arg,context.Push(arg))
                for attr in node.Info.Attributes do
                    this.Visit(attr,context.Push(attr))
                this.Visit(node.Body,context.Push(node.Body))
        with
        | :? StopIteration -> ()
        | _ -> reraise()

    member this.Visit(node:Tast.FunctionDecl, context:TastContext) =
        if accept context then
            for arg in node.Parameters do
                this.Visit(arg,context.Push(arg))
            for attr in node.Info.Attributes do
                this.Visit(attr,context.Push(attr))
            this.Visit(node.Body,context.Push(node.Body))

    member this.Visit(node:Tast.ValueDecl, context:TastContext) =
        if accept context then
            for attr in node.Info.Attributes do
                this.Visit(attr,context.Push(attr))
            this.Visit(node.Body,context.Push(node.Body))

    member this.Visit(node:Tast.TopLevelLambdaValue, context:TastContext) =
        if accept context then
            for arg in node.Parameters do
                this.Visit(arg,context.Push(arg))
            for attr in node.Info.Attributes do
                this.Visit(attr,context.Push(attr))
            this.Visit(node.Body,context.Push(node.Body))

    member this.Visit(node:Tast.Attribute, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.ParameterGroup<Tast.MFVInfo>, context:TastContext) =
        if accept context then
            match node with
            | Tast.NoParam -> ()
            | Tast.Param p -> this.Visit(p,context.Push(p))
            | Tast.TupleParam plist -> for p in plist do this.Visit(p,context.Push(p))

    member this.Visit(node:Tast.MFVInfo, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.Expression, context:TastContext) =
        if accept context then
            match node with
            | Tast.UnknownExpression _ -> ()
            | Tast.ValueExpr child -> this.Visit(child,context.Push(child))
            | Tast.ApplicationExpr child -> this.Visit(child,context.Push(child))
            | Tast.TypeLambdaExpr child -> this.Visit(child,context.Push(child))
            | Tast.DecisionTreeExpr child -> this.Visit(child,context.Push(child))
            | Tast.DecisionTreeSuccessExpr child -> this.Visit(child,context.Push(child))
            | Tast.LambdaExpr child -> this.Visit(child,context.Push(child))
            | Tast.IfThenElseExpr child -> this.Visit(child,context.Push(child))
            | Tast.LetExpr child -> this.Visit(child,context.Push(child))
            | Tast.CallExpr child -> this.Visit(child,context.Push(child))
            | Tast.NewObjectExpr child -> this.Visit(child,context.Push(child))
            | Tast.ThisValueExpr child -> this.Visit(child,context.Push(child))
            | Tast.BaseValueExpr child -> this.Visit(child,context.Push(child))
            | Tast.QuoteExpr child -> this.Visit(child,context.Push(child))
            | Tast.LetRecExpr child -> this.Visit(child,context.Push(child))
            | Tast.NewRecordExpr child -> this.Visit(child,context.Push(child))
            | Tast.NewAnonRecordExpr child -> this.Visit(child,context.Push(child))
            | Tast.AnonRecordGetExpr child -> this.Visit(child,context.Push(child))
            | Tast.FieldGetExpr child -> this.Visit(child,context.Push(child))
            | Tast.FieldSetExpr child -> this.Visit(child,context.Push(child))
            | Tast.NewUnionCaseExpr child -> this.Visit(child,context.Push(child))
            | Tast.UnionCaseGetExpr child -> this.Visit(child,context.Push(child))
            | Tast.UnionCaseSetExpr child -> this.Visit(child,context.Push(child))
            | Tast.UnionCaseTagExpr child -> this.Visit(child,context.Push(child))
            | Tast.UnionCaseTestExpr child -> this.Visit(child,context.Push(child))
            | Tast.NewTupleExpr child -> this.Visit(child,context.Push(child))
            | Tast.TupleGetExpr child -> this.Visit(child,context.Push(child))
            | Tast.CoerceExpr child -> this.Visit(child,context.Push(child))
            | Tast.NewArrayExpr child -> this.Visit(child,context.Push(child))
            | Tast.TypeTestExpr child -> this.Visit(child,context.Push(child))
            | Tast.AddressSetExpr child -> this.Visit(child,context.Push(child))
            | Tast.ValueSetExpr child -> this.Visit(child,context.Push(child))
            | Tast.DefaultValueExpr child -> this.Visit(child,context.Push(child))
            | Tast.ConstantExpr child -> this.Visit(child,context.Push(child))
            | Tast.AddressOfExpr child -> this.Visit(child,context.Push(child))
            | Tast.SequentialExpr child -> this.Visit(child,context.Push(child))
            | Tast.FastIntegerForLoopExpr child -> this.Visit(child,context.Push(child))
            | Tast.WhileLoopExpr child -> this.Visit(child,context.Push(child))
            | Tast.TryFinallyExpr child -> this.Visit(child,context.Push(child))
            | Tast.TryWithExpr child -> this.Visit(child,context.Push(child))
            | Tast.NewDelegateExpr child -> this.Visit(child,context.Push(child))
            | Tast.ILAsmExpr child -> this.Visit(child,context.Push(child))
            | Tast.ILFieldGetExpr child -> this.Visit(child,context.Push(child))
            | Tast.ILFieldSetExpr child -> this.Visit(child,context.Push(child))
            | Tast.ObjectExpr child -> this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ValueExpr, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.ApplicationExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Function
                this.Visit(child,context.Push(child))
            for child in node.Args do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.TypeLambdaExpr, context:TastContext) =
        if accept context then
            let child = node.Body
            this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.DecisionTreeExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Decision
                this.Visit(child,context.Push(child))
            for branch in node.Branches do
                let child = branch.Expr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.DecisionTreeSuccessExpr, context:TastContext) =
        if accept context then
            for child in node.Targets do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.LambdaExpr, context:TastContext) =
        if accept context then
            let child = node.Body
            this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.IfThenElseExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Condition
                this.Visit(child,context.Push(child))
            do
                let child = node.IfTrue
                this.Visit(child,context.Push(child))
            do
                let child = node.IfFalse
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.LetExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Binding
                this.Visit(child,context.Push(child))
            do
                let child = node.Body
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.Binding, context:TastContext) =
        if accept context then
            let child = node.Expression
            this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.CallExpr, context:TastContext) =
        if accept context then
            node.Expression |> Option.iter (fun child ->
                this.Visit(child,context.Push(child))
                )
            for child in node.Args do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.NewObjectExpr, context:TastContext) =
        if accept context then
            for child in node.Args do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ThisOrBaseValueExpr, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.QuoteExpr, context:TastContext) =
        accept context |> ignore
        //TODO process quotes

    member this.Visit(node:Tast.LetRecExpr, context:TastContext) =
        if accept context then
            for child in node.Definitions do
                this.Visit(child,context.Push(child))
            do
                let child = node.Body
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.NewRecordExpr, context:TastContext) =
        if accept context then
            for child in node.Args do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.AnonRecordGetExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Target
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.FieldGetExpr, context:TastContext) =
        if accept context then
            node.Target |> Option.iter (fun child ->
                this.Visit(child,context.Push(child))
                )

    member this.Visit(node:Tast.FieldSetExpr, context:TastContext) =
        if accept context then
            node.Target |> Option.iter (fun child ->
                this.Visit(child,context.Push(child))
                )
            do
                let child = node.SetExpr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.NewUnionCaseExpr, context:TastContext) =
        if accept context then
            for child in node.Exprs do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.UnionCaseGetExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Target
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.UnionCaseSetExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Target
                this.Visit(child,context.Push(child))
            do
                let child = node.SetExpr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.UnionCaseTagExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Target
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.UnionCaseTestExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Target
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.NewTupleExpr, context:TastContext) =
        if accept context then
            for child in node.Args do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.TupleGetExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Expression
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.CoerceExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Target
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.NewArrayExpr, context:TastContext) =
        if accept context then
            for child in node.Args do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.TypeTestExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Expr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.AddressSetExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Expr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ValueSetExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Expr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.DefaultValueExpr, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.ConstantExpr, context:TastContext) =
        accept context |> ignore

    member this.Visit(node:Tast.AddressOfExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Expr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.SequentialExpr, context:TastContext) =
        if accept context then
            do
                let child = node.First
                this.Visit(child,context.Push(child))
            do
                let child = node.Second
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.FastIntegerForLoopExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Start
                this.Visit(child,context.Push(child))
            do
                let child = node.Finish
                this.Visit(child,context.Push(child))
            do
                let child = node.Body
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.WhileLoopExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Guard
                this.Visit(child,context.Push(child))
            do
                let child = node.Body
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.TryFinallyExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Try
                this.Visit(child,context.Push(child))
            do
                let child = node.Finally
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.TryWithExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Try
                this.Visit(child,context.Push(child))
            do
                let child = node.V1Expr
                this.Visit(child,context.Push(child))
            do
                let child = node.Handler
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.NewDelegateExpr, context:TastContext) =
        if accept context then
            do
                let child = node.Expr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ILAsmExpr, context:TastContext) =
        if accept context then
            for child in node.ArgExprs do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ILFieldGetExpr, context:TastContext) =
        if accept context then
            node.Obj |> Option.iter (fun child ->
                this.Visit(child,context.Push(child))
                )

    member this.Visit(node:Tast.ILFieldSetExpr, context:TastContext) =
        if accept context then
            node.Obj |> Option.iter (fun child ->
                this.Visit(child,context.Push(child))
                )
            do
                let child = node.SetExpr
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ObjectExpr, context:TastContext) =
        if accept context then
            do
                let child = node.BaseCall
                this.Visit(child,context.Push(child))
            for child in node.Overrides do
                this.Visit(child,context.Push(child))
            for child in node.InterfaceImplementations do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ObjectExprOverride, context:TastContext) =
        if accept context then
            do
                let child = node.Body
                this.Visit(child,context.Push(child))
            for child in node.CurriedParameterGroups do
                this.Visit(child,context.Push(child))

    member this.Visit(node:Tast.ObjectExprInterfaceImplementation, context:TastContext) =
        if accept context then
            for child in node.Overrides do
                this.Visit(child,context.Push(child))

