module SonarAnalyzer.FSharp.Rules.S2077_ExecutingSqlQueries

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst

// =================================================
// #2077 Formatting SQL queries is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2077
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S2077"
    let messageFormat = "Make sure that executing SQL queries is safe here."
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

    let checkWithEarlyReturn f x =
        try
            f x
        with
        | :? EarlyReturn ->
            None

    // =================================
    // common logic
    // =================================

    /// return true if the entire expression tree is constant
    let rec isConstantExpression (expr:Tast.Expression) =
        let isConstantOpt exprOpt =  exprOpt |> Option.map isConstantExpression |> Option.defaultValue true

        match expr with
        | Tast.DefaultValueExpr _
        | Tast.ConstantExpr _
            -> true
        | Tast.ValueExpr _
        | Tast.UnknownExpression _
        | Tast.ApplicationExpr _
        | Tast.TypeLambdaExpr _
        | Tast.DecisionTreeExpr _
        | Tast.DecisionTreeSuccessExpr _
        | Tast.LetExpr _
        | Tast.NewObjectExpr _
        | Tast.ThisValueExpr _
        | Tast.BaseValueExpr _
        | Tast.QuoteExpr _
        | Tast.LetRecExpr _
        | Tast.NewRecordExpr _
        | Tast.NewAnonRecordExpr _
        | Tast.AnonRecordGetExpr _
        | Tast.FieldGetExpr _
        | Tast.FieldSetExpr _
        | Tast.NewUnionCaseExpr _
        | Tast.UnionCaseGetExpr _
        | Tast.UnionCaseSetExpr _
        | Tast.UnionCaseTagExpr _
        | Tast.UnionCaseTestExpr _
        | Tast.AddressSetExpr _
        | Tast.ValueSetExpr _
        | Tast.AddressOfExpr _
        | Tast.FastIntegerForLoopExpr _
        | Tast.WhileLoopExpr _
        | Tast.TryFinallyExpr _
        | Tast.TryWithExpr _
        | Tast.NewDelegateExpr _
        | Tast.ILAsmExpr _
        | Tast.ILFieldGetExpr _
        | Tast.ILFieldSetExpr _
        | Tast.ObjectExpr _
            -> false
        | Tast.CallExpr expr ->
            isConstantOpt expr.Expression
            && (expr.Args |> List.forall isConstantExpression )
        | Tast.LambdaExpr expr ->
            isConstantExpression expr.Body
        | Tast.IfThenElseExpr expr ->
            isConstantExpression expr.Condition
            && isConstantExpression expr.IfTrue
            && isConstantExpression expr.IfFalse
        | Tast.NewTupleExpr expr ->
            expr.Args |> List.forall isConstantExpression
        | Tast.TupleGetExpr expr ->
            isConstantExpression expr.Expression
        | Tast.CoerceExpr expr ->
            isConstantExpression expr.Target
        | Tast.NewArrayExpr expr ->
            expr.Args |> List.forall isConstantExpression
        | Tast.TypeTestExpr expr ->
            isConstantExpression expr.Expr
        | Tast.SequentialExpr expr ->
            isConstantExpression expr.First
            && isConstantExpression expr.Second

    let stringOperations = ["op_Addition"; "Concat"; "Format"]

    /// sprintf is special kind of ApplicationExpr
    let isSprintf (expr:Tast.ApplicationExpr) =
        match expr.Function with
        | Tast.LetExpr expr ->
            match expr.Binding.Expression with
            | Tast.CallExpr expr ->
                expr.Member = WellKnownMember.sprintf
            | _ ->
                false
        | _ ->
            false

    let isConcatenatedOrFormatted (node:Tast.Expression) =
        match node with
        | Tast.CallExpr expr ->
            stringOperations |> List.contains expr.Member.CompiledName
        | Tast.ApplicationExpr expr ->
            isSprintf expr
        | _ ->
            false

    /// An arg is bad if
    /// * it is a string
    /// * it uses Concat, Format etc
    /// * it is not a constant expression
    let isArgBad (arg:Tast.Expression) (argType:Tast.FSharpType) =
        (argType |> isType WellKnownType.string) // arg must be string
        && (isConcatenatedOrFormatted arg) // and combined
        && not (isConstantExpression arg) // if whole arg is a constant, then it's not bad

    /// check whether the arg at an index is bad
    let isArgNBad (args:Tast.Expression list) (argTypes:Tast.FSharpType list) n =
        if args.Length > n then
            isArgBad args.[n] argTypes.[n]
        else
            false


    // =================================
    // check ExecuteSqlCommand
    // =================================

    /// Check that methods such as ExecuteSqlCommand are used OK
    let checkExecuteSqlCommand (ctx:TastContext) =
        let checkedNames = [
            "FromSql"
            "ExecuteSqlCommandAsync"
            "ExecuteSqlCommand"
            ]

        let call, _declaringEntity =
            option {
                let! call = ctx.Try<Tast.CallExpr>()
                let! declaringEntity = call.Member.DeclaringEntity
                return call,declaringEntity
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // check the member being called
        if checkedNames |> List.contains call.Member.CompiledName |> not then raise EarlyReturn

        // is arg0 or arg1 bad
        let isArgNBad = isArgNBad call.Args call.ArgTypes
        if (isArgNBad 0) || (isArgNBad 1) then
            Diagnostic.Create(rule, call.Location, call.Member.CompiledName) |> Some
        else
            None

    // =================================
    // check Constructors like SqlCommand
    // =================================

    let classNames = [
        "RawSqlString"
        "SqlCommand"
        "SqlDataAdapter"
        "OdbcCommand"
        "OdbcDataAdapter"
        "SqlCeCommand"
        "SqlCeDataAdapter"
        "OracleCommand"
        "OracleDataAdapter"
        ]

    let isClassNameTracked className =
        classNames |> List.contains className

    /// Check that constructors such as SqlCommand are used OK
    let checkConstructors (ctx:TastContext) =

        let newObjExpr, declaringEntity =
            option {
                let! newObjExpr = ctx.Try<Tast.NewObjectExpr>()
                let! declaringEntity = newObjExpr.Ctor.DeclaringEntity
                return newObjExpr,declaringEntity
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // is the class one of the ones we're interested in?
        if not (isClassNameTracked declaringEntity.CompiledName) then raise EarlyReturn

        // some args available?
        if newObjExpr.Args.IsEmpty then raise EarlyReturn

        // is arg0 bad
        let isArgNBad = isArgNBad newObjExpr.Args newObjExpr.ArgTypes
        if isArgNBad 0 |> not then raise EarlyReturn

        Diagnostic.Create(rule, newObjExpr.Location, declaringEntity.DisplayName) |> Some


    // =================================
    // check setting of CommandText property
    // =================================

    /// Check that constructors such as SqlCommand are used OK
    let checkCommandTextSetter (ctx:TastContext) =

        let checkedNames = [
            "set_CommandText"
            ]

        let call, _declaringEntity =
            option {
                let! call = ctx.Try<Tast.CallExpr>()
                let! declaringEntity = call.Member.DeclaringEntity
                return call,declaringEntity
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // check the member being called
        if checkedNames |> List.contains call.Member.CompiledName |> not then raise EarlyReturn

        // is the set value bad?
        let isArgNBad = isArgNBad call.Args call.ArgTypes
        if (isArgNBad 0) then
            Diagnostic.Create(rule, call.Location, call.Member.CompiledName) |> Some
        else
            None



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
        (checkWithEarlyReturn checkExecuteSqlCommand)
        <|> (checkWithEarlyReturn checkConstructors)
        <|> (checkWithEarlyReturn checkCommandTextSetter)
    rule ctx
