module FSharpAst.UnitTest.TastVisitor

open NUnit.Framework
open FSharpAst

open Serilog
open Serilog.Events

// set up logging
do Serilog.Log.Logger <-
    Serilog.LoggerConfiguration()
        .Enrich.FromLogContext()
        .MinimumLevel.Debug()
        .WriteTo.Console(
            LogEventLevel.Verbose,
            "{NewLine}{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();

let logger = Serilog.Log.Logger


//let config = TransformerConfig.Default
let config = {TransformerConfig.Default with UseEmptyLocation=true}

let translateText text = FSharpAst.TextApi.translateText config text

type OptionBuilder() =
    member this.Return(x) = Some x
    member this.ReturnFrom(x) = x
    member this.Bind(x,f) = x |> Option.bind f
    member this.Zero(x) = None
let option = OptionBuilder()

let tryMatchType<'T> (node:obj) =
    match node with
    | :? 'T as node -> Some node
    | _ -> None

/// call with "myType |> isType KnownType.string"
let isType typeDesc (node:Tast.FSharpType) =
    match node with
    /// A type expression we don't know how to handle
    | Tast.NamedType namedType ->
        namedType.Descriptor = typeDesc
    | Tast.UnknownFSharpType _
    | Tast.VariableType _
    | Tast.TupleType _
    | Tast.FunctionType _
    | Tast.ArrayType _
    | Tast.OtherType _
    | Tast.ByRefType _ -> false

let tryParentExpr (context:TastContext) =
    context.TryAncestorOrSelf<Tast.Expression>()

let tryParentCallExpr (context:TastContext) =
    context.TryAncestorOrSelf<Tast.CallExpr>()

// Detect if this context is used in an argument to a CallExpr.
// If so, return the arg index or None if not found
let tryArgumentIndex (context:TastContext) =

    let isChildOfArg (callExprContext:TastContext) (argExpr:Tast.Expression) =
        let argExprContext = callExprContext.Push(argExpr)
        let mutable found = false
        let accept argChild =
            found <- argChild.Node = context.Node
            true // keep going
        let v = TastVisitor(accept)
        v.Visit(argExpr,argExprContext)
        found

    option {
    let! callCtx,callExp = tryParentCallExpr context
    return!
        callExp.Args
        |> List.tryFindIndex (isChildOfArg callCtx)
    }

let isInArgument (context:TastContext) =
    (tryArgumentIndex context).IsSome

let resultValue = function
    | Ok ok -> ok
    | Error _ -> failwith "expected OK"

let systemStringType = WellKnownType.SystemString
let stringType = WellKnownType.string
let intType = WellKnownType.int


/// Find all constant strings
[<Test>]
let constantStrings() =

    let text = """
module MyModule =
    let test x = x

let a = "a"   // match
let b = MyModule.test("b")  // match
let two = MyModule.test(2) // no match
"""

    let tast = translateText text

    let results = ResizeArray()
    let accept (context:TastContext) =
        option {
            let! node = tryMatchType<Tast.ConstantExpr> context.Node
            if node.Type |> isType stringType then
                results.Add node.Value
        } |> ignore
        true // keep going

    let v = TastVisitor accept
    v.Visit(resultValue tast)

    let expected = ["a"; "b"] |> sprintf "%A"
    let actual = results |> Seq.toList |> sprintf "%A"
    Assert.AreEqual(expected,actual)


/// Find all constant strings used as a parameter
[<Test>]
let constantStringsUsedAsArgs() =

    let text = """
module MyModule =
    let test x = x

let a = "a"  // no match
let b = MyModule.test("b1" + "b2")  // Match
let c = System.String.Format("{0}","c") // Matches both format string and arg
let d = "d".ToString()  // no match
"""

    let tast = translateText text

    let results = ResizeArray()
    let accept (context:TastContext) =
        option {
            let! node = tryMatchType<Tast.ConstantExpr> context.Node
            if isInArgument context && node.Type |> isType stringType then
                results.Add node.Value
        } |> ignore
        true // keep going

    let v = TastVisitor accept
    v.Visit(resultValue tast)

    let expected = ["b1"; "b2"; "{0}";"c"] |> sprintf "%A"
    let actual = results |> Seq.toList |> sprintf "%A"
    Assert.AreEqual(expected,actual)

/// Find all constant strings used as a parameter
[<Test>]
let constantStringsUsedAsArgToStringFormat() =

    let text = """
module MyModule =
    let test x = x

let a = "a"
let b = MyModule.test("b" + "b1")       // no match
let c = System.String.Format("{0}","c") // match
let fs = System.String.Format("fs")       // don't match format string
let d = "d".ToString()
"""

    let tast = translateText text
    let stringFormatDescriptor : Tast.MemberDescriptor = {
        DeclaringEntity = Some systemStringType
        CompiledName = "Format"
        DisplayName = "Format"
        }

    let results = ResizeArray()
    let accept (context:TastContext) =
        option {
            let! node = tryMatchType<Tast.ConstantExpr> context.Node
            let! _callCtx, callNode = tryParentCallExpr context
            if callNode.Member = stringFormatDescriptor then
                let! argumentIndex = tryArgumentIndex context
                if argumentIndex >= 1 && node.Type |> isType stringType then
                    results.Add node.Value
        } |> ignore
        true // keep going

    let v = TastVisitor accept
    v.Visit(resultValue tast)

    let expected = [|"c"|]
    let actual = results |> Seq.toArray
    Assert.AreEqual(expected,actual)
