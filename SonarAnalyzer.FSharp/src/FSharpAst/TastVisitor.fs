namespace FSharpAst

open System


/// An exception can be thrown to stop iterating
exception StopIteration

/// A context when visiting a Tast for a file
type TastContext<'NodeType> = {
    Node : 'NodeType
    Ancestors : obj list
    }
    with

    /// Push a new child node into the context.
    member this.Push(childNode :'a) : TastContext<'a> =
        let newAncestors = (box this.Node) :: this.Ancestors
        {Node=childNode; Ancestors=newAncestors}

    /// Pop the context
    member this.TryPop() : TastContext<obj> option =
        match this.Ancestors with
        | [] ->
            None
        | head::tail ->
            Some {Node=head; Ancestors=tail}

    /// Convert a TastContext<_> into a TastContext<obj>
    member this.Box :TastContext<obj> =
        {Node=box this.Node; Ancestors=this.Ancestors}

    /// Try to convert a TastContext<_> into a TastContext<'T>
    member this.TryCast<'T>() :TastContext<'T> option =
        match box this.Node with
        | :? 'T as node ->
            Some {Node=node; Ancestors=this.Ancestors}
        | _ ->
            None

/// An abbreviation for where node is of type obj
type TastContext = TastContext<obj>


// For each node in the tree, run the "accept" function. If it returns true, keep going, else stop.
type TastVisitor(accept: TastContext<obj> -> bool) =

    member this.Visit(node:Tast.ImplementationFile) =
        let context = {Node=node; Ancestors=[]}
        for child in node.Decls do
            this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ImplementationFileDecl>) =
        if accept context.Box then
            let node = context.Node
            match node with
            | Tast.Entity child -> this.Visit(context.Push(child))
            | Tast.MfvDecl child -> this.Visit(context.Push(child))
            | Tast.InitAction child -> this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.Entity>) =
        if accept context.Box then
            let node = context.Node
            match node with
            | Tast.Namespace child -> this.Visit(context.Push(child))
            | Tast.Module child -> this.Visit(context.Push(child))
            | Tast.TypeEntity child -> this.Visit(context.Push(child))
            | Tast.UnhandledEntity _ -> ()

    member this.Visit(context:TastContext<Tast.Namespace>) =
        if accept context.Box then
            let node = context.Node
            for child in node.SubDecls do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.Module>) =
        if accept context.Box then
            let node = context.Node
            for child in node.SubDecls do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.TypeEntity>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.MfvDecl>) =
        if accept context.Box then
            let node = context.Node
            match node with
            | Tast.MemberDecl child -> this.Visit(context.Push(child))
            | Tast.FunctionDecl child -> this.Visit(context.Push(child))
            | Tast.ValueDecl child -> this.Visit(context.Push(child))
            | Tast.UnhandledMemberOrFunctionOrValue _ -> ()
            | Tast.TopLevelLambdaValueDecl child -> this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.InitAction>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.MemberDecl>) =
        try
            if accept context.Box then
                let node = context.Node
                for arg in node.Parameters do
                    this.Visit(context.Push(arg))
                for attr in node.Info.Attributes do
                    this.Visit(context.Push(attr))
                this.Visit(context.Push(node.Body))
        with
        | :? StopIteration -> ()
        | _ -> reraise()

    member this.Visit(context:TastContext<Tast.FunctionDecl>) =
        if accept context.Box then
            let node = context.Node
            for arg in node.Parameters do
                this.Visit(context.Push(arg))
            for attr in node.Info.Attributes do
                this.Visit(context.Push(attr))
            this.Visit(context.Push(node.Body))

    member this.Visit(context:TastContext<Tast.ValueDecl>) =
        if accept context.Box then
            let node = context.Node
            for attr in node.Info.Attributes do
                this.Visit(context.Push(attr))
            this.Visit(context.Push(node.Body))

    member this.Visit(context:TastContext<Tast.TopLevelLambdaValueDecl>) =
        if accept context.Box then
            let node = context.Node
            for arg in node.Parameters do
                this.Visit(context.Push(arg))
            for attr in node.Info.Attributes do
                this.Visit(context.Push(attr))
            this.Visit(context.Push(node.Body))

    member this.Visit(context:TastContext<Tast.Attribute>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.ParameterGroup<Tast.MfvInfo>>) =
        if accept context.Box then
            let node = context.Node
            match node with
            | Tast.NoParam -> ()
            | Tast.Param p -> this.Visit(context.Push(p))
            | Tast.TupleParam plist -> for p in plist do this.Visit(context.Push(p))

    member this.Visit(context:TastContext<Tast.MfvInfo>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.Expression>) =
        if accept context.Box then
            let node = context.Node
            match node with
            | Tast.UnknownExpression _ -> ()
            | Tast.ValueExpr child -> this.Visit(context.Push(child))
            | Tast.ApplicationExpr child -> this.Visit(context.Push(child))
            | Tast.TypeLambdaExpr child -> this.Visit(context.Push(child))
            | Tast.DecisionTreeExpr child -> this.Visit(context.Push(child))
            | Tast.DecisionTreeSuccessExpr child -> this.Visit(context.Push(child))
            | Tast.LambdaExpr child -> this.Visit(context.Push(child))
            | Tast.IfThenElseExpr child -> this.Visit(context.Push(child))
            | Tast.LetExpr child -> this.Visit(context.Push(child))
            | Tast.CallExpr child -> this.Visit(context.Push(child))
            | Tast.NewObjectExpr child -> this.Visit(context.Push(child))
            | Tast.ThisValueExpr child -> this.Visit(context.Push(child))
            | Tast.BaseValueExpr child -> this.Visit(context.Push(child))
            | Tast.QuoteExpr child -> this.Visit(context.Push(child))
            | Tast.LetRecExpr child -> this.Visit(context.Push(child))
            | Tast.NewRecordExpr child -> this.Visit(context.Push(child))
            | Tast.NewAnonRecordExpr child -> this.Visit(context.Push(child))
            | Tast.AnonRecordGetExpr child -> this.Visit(context.Push(child))
            | Tast.FieldGetExpr child -> this.Visit(context.Push(child))
            | Tast.FieldSetExpr child -> this.Visit(context.Push(child))
            | Tast.NewUnionCaseExpr child -> this.Visit(context.Push(child))
            | Tast.UnionCaseGetExpr child -> this.Visit(context.Push(child))
            | Tast.UnionCaseSetExpr child -> this.Visit(context.Push(child))
            | Tast.UnionCaseTagExpr child -> this.Visit(context.Push(child))
            | Tast.UnionCaseTestExpr child -> this.Visit(context.Push(child))
            | Tast.NewTupleExpr child -> this.Visit(context.Push(child))
            | Tast.TupleGetExpr child -> this.Visit(context.Push(child))
            | Tast.CoerceExpr child -> this.Visit(context.Push(child))
            | Tast.NewArrayExpr child -> this.Visit(context.Push(child))
            | Tast.TypeTestExpr child -> this.Visit(context.Push(child))
            | Tast.AddressSetExpr child -> this.Visit(context.Push(child))
            | Tast.ValueSetExpr child -> this.Visit(context.Push(child))
            | Tast.DefaultValueExpr child -> this.Visit(context.Push(child))
            | Tast.ConstantExpr child -> this.Visit(context.Push(child))
            | Tast.AddressOfExpr child -> this.Visit(context.Push(child))
            | Tast.SequentialExpr child -> this.Visit(context.Push(child))
            | Tast.FastIntegerForLoopExpr child -> this.Visit(context.Push(child))
            | Tast.WhileLoopExpr child -> this.Visit(context.Push(child))
            | Tast.TryFinallyExpr child -> this.Visit(context.Push(child))
            | Tast.TryWithExpr child -> this.Visit(context.Push(child))
            | Tast.NewDelegateExpr child -> this.Visit(context.Push(child))
            | Tast.ILAsmExpr child -> this.Visit(context.Push(child))
            | Tast.ILFieldGetExpr child -> this.Visit(context.Push(child))
            | Tast.ILFieldSetExpr child -> this.Visit(context.Push(child))
            | Tast.ObjectExpr child -> this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ValueExpr>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.ApplicationExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Function
                this.Visit(context.Push(child))
            for child in node.Args do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.TypeLambdaExpr>) =
        if accept context.Box then
            let node = context.Node
            let child = node.Body
            this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.DecisionTreeExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Decision
                this.Visit(context.Push(child))
            for branch in node.Branches do
                let child = branch.Expr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.DecisionTreeSuccessExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Targets do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.LambdaExpr>) =
        if accept context.Box then
            let node = context.Node
            let child = node.Body
            this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.IfThenElseExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Condition
                this.Visit(context.Push(child))
            do
                let child = node.IfTrue
                this.Visit(context.Push(child))
            do
                let child = node.IfFalse
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.LetExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Binding
                this.Visit(context.Push(child))
            do
                let child = node.Body
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.Binding>) =
        if accept context.Box then
            let node = context.Node
            let child = node.Expression
            this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.CallExpr>) =
        if accept context.Box then
            let node = context.Node
            node.Instance |> Option.iter (fun child ->
                this.Visit(context.Push(child))
                )
            for child in node.Args do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.NewObjectExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Args do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ThisOrBaseValueExpr>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.QuoteExpr>) =
        accept context.Box |> ignore
        //TODO process quotes

    member this.Visit(context:TastContext<Tast.LetRecExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Definitions do
                this.Visit(context.Push(child))
            do
                let child = node.Body
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.NewRecordExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Args do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.AnonRecordGetExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Target
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.FieldGetExpr>) =
        if accept context.Box then
            let node = context.Node
            node.Target |> Option.iter (fun child ->
                this.Visit(context.Push(child))
                )

    member this.Visit(context:TastContext<Tast.FieldSetExpr>) =
        if accept context.Box then
            let node = context.Node
            node.Target |> Option.iter (fun child ->
                this.Visit(context.Push(child))
                )
            do
                let child = node.SetExpr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.NewUnionCaseExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Exprs do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.UnionCaseGetExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Target
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.UnionCaseSetExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Target
                this.Visit(context.Push(child))
            do
                let child = node.SetExpr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.UnionCaseTagExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Target
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.UnionCaseTestExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Target
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.NewTupleExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Args do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.TupleGetExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Expression
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.CoerceExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Target
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.NewArrayExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Args do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.TypeTestExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Expr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.AddressSetExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Expr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ValueSetExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Expr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.DefaultValueExpr>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.ConstantExpr>) =
        accept context.Box |> ignore

    member this.Visit(context:TastContext<Tast.AddressOfExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Expr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.SequentialExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.First
                this.Visit(context.Push(child))
            do
                let child = node.Second
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.FastIntegerForLoopExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Start
                this.Visit(context.Push(child))
            do
                let child = node.Finish
                this.Visit(context.Push(child))
            do
                let child = node.Body
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.WhileLoopExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Guard
                this.Visit(context.Push(child))
            do
                let child = node.Body
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.TryFinallyExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Try
                this.Visit(context.Push(child))
            do
                let child = node.Finally
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.TryWithExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Try
                this.Visit(context.Push(child))
            do
                let child = node.V1Expr
                this.Visit(context.Push(child))
            do
                let child = node.Handler
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.NewDelegateExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Expr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ILAsmExpr>) =
        if accept context.Box then
            let node = context.Node
            for child in node.ArgExprs do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ILFieldGetExpr>) =
        if accept context.Box then
            let node = context.Node
            node.Obj |> Option.iter (fun child ->
                this.Visit(context.Push(child))
                )

    member this.Visit(context:TastContext<Tast.ILFieldSetExpr>) =
        if accept context.Box then
            let node = context.Node
            node.Obj |> Option.iter (fun child ->
                this.Visit(context.Push(child))
                )
            do
                let child = node.SetExpr
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ObjectExpr>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.BaseCall
                this.Visit(context.Push(child))
            for child in node.Overrides do
                this.Visit(context.Push(child))
            for child in node.InterfaceImplementations do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ObjectExprOverride>) =
        if accept context.Box then
            let node = context.Node
            do
                let child = node.Body
                this.Visit(context.Push(child))
            for child in node.CurriedParameterGroups do
                this.Visit(context.Push(child))

    member this.Visit(context:TastContext<Tast.ObjectExprInterfaceImplementation>) =
        if accept context.Box then
            let node = context.Node
            for child in node.Overrides do
                this.Visit(context.Push(child))

