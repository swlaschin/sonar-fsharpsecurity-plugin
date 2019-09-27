module SonarAnalyzer.FSharp.Rules.S4784_UsingRegularExpressions

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst

// =================================================
// #4784 Using regular expressions is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4784
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4784";
    let messageFormat = "Make sure that using a regular expression is safe here.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

    let checkWithEarlyReturn f x =
        try
            f x
        with
        | :? EarlyReturn ->
            None

    /// traverses the tree and returns a list of all possible string type arguments which are constants.
    let rec getPossibleConstantStringArguments (expr:Tast.Expression) : string list =
        let getConstantOpt exprOpt =  exprOpt |> Option.map getPossibleConstantStringArguments |> Option.defaultValue [""]

        match expr with
        | Tast.DefaultValueExpr def -> 
            match def.Type with
            | Tast.FSharpType.NamedType n when n.Descriptor = WellKnownType.string 
                -> "" |> List.singleton
            | _ -> []
        | Tast.ConstantExpr con -> 
            match con.Type with
            | Tast.FSharpType.NamedType n when n.Descriptor = WellKnownType.string 
                -> con.Value |> string |> List.singleton
            | _ -> []
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
            -> []
        | Tast.CallExpr expr ->
            getConstantOpt expr.Expression
            @ (expr.Args |> List.collect getPossibleConstantStringArguments)
        | Tast.LambdaExpr expr ->
            getPossibleConstantStringArguments expr.Body
        | Tast.IfThenElseExpr expr ->
            getPossibleConstantStringArguments expr.Condition
            @ getPossibleConstantStringArguments expr.IfTrue
            @ getPossibleConstantStringArguments expr.IfFalse
        | Tast.NewTupleExpr expr ->
            expr.Args |> List.collect getPossibleConstantStringArguments
        | Tast.TupleGetExpr expr ->
            getPossibleConstantStringArguments expr.Expression
        | Tast.CoerceExpr expr ->
            getPossibleConstantStringArguments expr.Target
        | Tast.NewArrayExpr expr ->
            expr.Args |> List.collect getPossibleConstantStringArguments
        | Tast.TypeTestExpr expr ->
            getPossibleConstantStringArguments expr.Expr
        | Tast.SequentialExpr expr ->
            getPossibleConstantStringArguments expr.First
            @ getPossibleConstantStringArguments expr.Second

    /// checks if the string is 3 or more chars long
    let is3OrMoreChars (s: string) = s.Length > 2

    /// checks if the string contains two or more instances of: * + {
    let containsKeyCharacters (s: string) = 
        let keyChars = [ '*'; '+'; '{' ]
        let numberOfKeyChars = s |> Seq.sumBy (fun c -> if keyChars |> List.contains c then 1 else 0)
        numberOfKeyChars > 1

    /// An arg is bad if it is
    /// * a string
    /// * a constant expression    
    /// * 3 or more chars long
    let isArgBad (arg:Tast.Expression) (argType:Tast.FSharpType) =
        if not (argType |> isType WellKnownType.string) then raise EarlyReturn

        let argumentList = getPossibleConstantStringArguments arg
        argumentList |> List.exists (fun arg -> is3OrMoreChars arg && containsKeyCharacters arg)

    /// check whether the arg at an index is bad
    let isArgNBad (args:Tast.Expression list) (argTypes:Tast.FSharpType list) n =
        if args.Length > n then
            isArgBad args.[n] argTypes.[n]
        else
            false

    let checkedDeclaringEntity : Tast.NamedTypeDescriptor option =
        Some { AccessPath = "System.Text.RegularExpressions"; DisplayName = "Regex"; CompiledName = "Regex" }

    let checkForRegexCtor (ctx: TastContext) =
        let newObjExpr = 
            ctx.Try<Tast.NewObjectExpr>()
            |> Option.defaultWith (fun _ -> raise EarlyReturn)
        
        // is the class one we're interested in?
        if newObjExpr.Ctor.DeclaringEntity <> checkedDeclaringEntity then raise EarlyReturn

        // are we calling the constructor?
        if newObjExpr.Ctor.CompiledName <> ".ctor" then raise EarlyReturn

        // some args available?
        if newObjExpr.Args.IsEmpty then raise EarlyReturn

        // is arg0 bad?
        let isArgNBad = isArgNBad newObjExpr.Args newObjExpr.ArgTypes
        if isArgNBad 0 |> not then raise EarlyReturn

        Diagnostic.Create(rule, newObjExpr.Location, newObjExpr.Ctor.DisplayName) |> Some

    let checkForStaticInvocation (ctx: TastContext) =
        let checkedNames = [
            "IsMatch"
            "Match"
            "Matches"
            "Replace"
            "Split"
            ]

        let call =
            ctx.Try<Tast.CallExpr>()
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // is the class one we're interested in?
        if call.Member.DeclaringEntity <> checkedDeclaringEntity then raise EarlyReturn

        // is the member one were interested in?
        if checkedNames |> List.contains call.Member.CompiledName |> not then raise EarlyReturn

        // is it static?
        if call.Expression.IsSome then raise EarlyReturn

        // some args available?
        if call.Args.IsEmpty then raise EarlyReturn

        // is arg0 bad?
        let isArgNBad = isArgNBad call.Args call.ArgTypes
        if isArgNBad 1 |> not then raise EarlyReturn

        Diagnostic.Create(rule, call.Location, call.Member.DisplayName) |> Some

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
        (checkWithEarlyReturn checkForRegexCtor)
        <|> (checkWithEarlyReturn checkForStaticInvocation)
    rule ctx
