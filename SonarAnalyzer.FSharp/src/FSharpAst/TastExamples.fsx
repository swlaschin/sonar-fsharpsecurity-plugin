
(* =====================================================
To work with this project interactively, you need access to FSharp.Compiler.Service.dll and other dlls from NuGet

1) Install paket in the "sonaranalyzer-fsharp" directory
    cd sonaranalyzer-fsharp\SonarAnalyzer.FSharp
    dotnet tool install --tool-path ".paket" Paket --add-source https://api.nuget.org/v3/index.json

2) Run paket to install the packages
    .paket\paket.exe install

===================================================== *)


#r @"..\..\packages\FSharp.Compiler.Service\lib\netstandard2.0\FSharp.Compiler.Service.dll"
#r @"..\..\packages\Serilog\lib\netstandard2.0\Serilog.dll"
#r @"bin\Debug\netstandard2.0\FSharpAst.dll"
#r @"netstandard.dll"


open FSharpAst

let config = TransformerConfig.Default
//let config = {TransformerConfig.Default with UseEmptyLocation=true}
let translateText text = FSharpAst.TextApi.translateText config text

// select the first Mfv in the Tast
let firstMfv (file:Tast.ImplementationFile) =
    let rootDecl = file.Decls.[0]
    match rootDecl with
    | Tast.Entity (Tast.Module m) ->
        m.SubDecls
        |> List.pick (function
            | Tast.MfvDecl mfv -> Some mfv
            | _ -> None)
    | _ -> failwith "expecting a module"

// select the value decl with name "z"
let getZ (file:Tast.ImplementationFile) =
    let rootDecl = file.Decls.[0]
    match rootDecl with
    | Tast.Entity (Tast.Module m) ->
        m.SubDecls
        |> List.find (function
            | Tast.MfvDecl (Tast.ValueDecl mfv) -> mfv.Info.Name = "z"
            | _ -> false)
    | _ -> failwith "expecting a module"

// ---------------------------
// constant expression
// ---------------------------

module ConstantExpr =
    let text = """
let z = 1
"""

    let tast = translateText text |> Result.map getZ
    printfn "%A" tast

// ---------------------------
// call expressions
// ---------------------------

module CallValue =

    """
module MyModule =
    let x = 1
let z = MyModule.x
"""

    |> translateText
    |> Result.map getZ
    |> printfn "%A"

module CallFunction =

    """
module MyModule =
    let test (x:int) = x
let z = MyModule.test(1)
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"


module CallGenericFunction =

    """
module MyModule =
    let test (x:'a) = x
let z = MyModule.test(1)
"""

    // tast will have MethodTypeArgs now
    |> translateText
    |> Result.map getZ
    |> printfn "%A"

module CallStaticMember =

    """
type MyClass() =
    static member Test(x) = x
let z = MyClass.Test(1)
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"

module CallGenericStaticMember =

    """
type MyClass<'a>() =
    static member Test(x) = x
let z = MyClass<string>.Test(1)
"""

    // tast will have ClassTypeArgs now
    |> translateText
    |> Result.map getZ
    |> printfn "%A"

module CallInstanceMember =

    """
type MyClass() =
    member this.Test(x) = x
let myClass = MyClass()
let z = myClass.Test(1)
"""

    |> translateText
    |> Result.map getZ
    |> printfn "%A"


// ---------------------------
// constructor expressions
// ---------------------------

module NewObjectExprNoParam =

    """
type MyClass() = class end
let z = MyClass()
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: there is one arg to .ctor -- unit

module NewObjectExprOneParam =

    """
type MyClass(x:int) = class end
let z = MyClass(1)
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: there is one arg to .ctor -- unit

module NewObjectExprTypeParam =

    """
type MyClass<'a>(x:int) = class end
let z = MyClass<string>(1)
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: TypeArgs is set now


// ---------------------------
// getter and setter expressions
// ---------------------------

module GetProperty =

    """
type MyClass() =
    member val Prop = 1 with get,set
let myClass = MyClass()
let z = myClass.Prop
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a call to "get_Prop"

module SetProperty =

    """
type MyClass() =
    member val Prop = 1 with get,set
let myClass = MyClass()
let z = myClass.Prop <- 1
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a call to "set_Prop"

module GetField =

    """
type MyClass() =
    [<DefaultValue>] val mutable myField : int
let myClass = MyClass()
let z = myClass.myField
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a "FieldGetExpr" rather than a "Call" expr

module SetField =

    """
type MyClass() =
    [<DefaultValue>] val mutable myField : int
let myClass = MyClass()
let z = myClass.myField <- 1
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a "FieldSetExpr" rather than a "Call" expr

module GetArrayItem =

    """
let arr = [|1|]
let z = arr.[0]
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a "IntrinsicFunctions.GetArray" rather than a "Call" expr

module SetArrayItem =

    """
let arr = [|1|]
let z = arr.[0] <- 2
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a "IntrinsicFunctions.SetArray" rather than a "Call" expr

module GetDictionaryItem =

    """
let dict = System.Collections.Generic.Dictionary<string,int>()
dict.Add("a",1)
let z = dict.["a"]
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a "get_Item" call

module SetDictionaryItem =

    """
let dict = System.Collections.Generic.Dictionary<string,int>()
dict.Add("a",1)
let z = dict.["a"] <- 1
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    // NOTE: this is a "set_Item" call

// ---------------------------
// Construct and set at the same time
// ---------------------------

module ConstructAndSet =
    """
type MyClass(i:int) =
    member val Prop = 1 with get,set
let z = MyClass(1,Prop=2)
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"

    (*
    NOTE: this is translated into three steps
        let returnVal = MyClass(1) in
        returnVal.set_Prop(2)
        returnVal
    *)

// ---------------------------
// flags
// ---------------------------


module Flags =

    """
open System.Reflection
let z = BindingFlags.NonPublic ||| BindingFlags.Static
"""
    |> translateText
    |> Result.map getZ
    |> printfn "%A"
    (*
    ConstantExpr with Type=System.Reflection.BindingFlags, value = 32, 8 etc
    *)