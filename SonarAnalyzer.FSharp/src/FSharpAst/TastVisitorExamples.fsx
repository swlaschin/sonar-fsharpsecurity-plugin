
(* =====================================================
To work with this project interactively, you need access to FSharp.Compiler.Service.dll from NuGet

1) Install paket in the "sonaranalyzer-fsharp" directory
    cd sonaranalyzer-fsharp
    dotnet tool install --tool-path ".paket" Paket --add-source https://api.nuget.org/v3/index.json

2) Run paket to install the packages
    .paket\paket.exe install

===================================================== *)


#r @"..\..\packages\FSharp.Compiler.Service\lib\netstandard2.0\FSharp.Compiler.Service.dll"
#r @"bin\Debug\netstandard2.0\FSharpAst.dll"
#r @"netstandard"

open FSharpAst

//let config = TransformerConfig.Default
let config = {TransformerConfig.Default with UseEmptyLocation=true}
let translateText text = FSharpAst.TextApi.translateText config text

let tryMatchType<'T> (node:obj) =
    match node with
    | :? 'T as node -> Some node
    | _ -> None

let matchNamedType (node:Tast.FSharpType) typeDesc =
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


let resultValue = function
    | Ok ok -> ok
    | Error _ -> failwith "expected OK"

// ---------------------------
// function call
// ---------------------------

let stringType : Tast.NamedTypeDescriptor =
    {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "string"; CompiledName = "string"}
let intType : Tast.NamedTypeDescriptor =
    {AccessPath = "Microsoft.FSharp.Core"; DisplayName = "int"; CompiledName = "int"}

let text = """
let s = "s"
module MyModule =
    let test x = x
let z = MyModule.test("p")
let z2 = MyModule.test(2)
"""

let tast = translateText text

let results = ResizeArray()
let accept (context:TastContext) =
    tryMatchType<Tast.ConstantExpr> context.Node
    |> Option.map (fun node ->
        if matchNamedType node.Type intType then
            results.Add node.Value
        )
    |> ignore
    true // keep going

//let accept (context:TastContext)=
//    printfn "%A" context.Node

let v = TastVisitor accept
v.Visit(resultValue tast)
results |> Seq.toList |> printfn "%A"
