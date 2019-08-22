
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

open FSharpAst

//let config = TransformerConfig.Default
let config = {TransformerConfig.Default with UseEmptyLocation=true}
let translateText text = FSharpAst.TextApi.translateText config text

// ---------------------------
// function call
// ---------------------------

let text = """
module MyModule =
    let test x = x
let z = MyModule.test(1)
"""

let tast = translateText text

// ---------------------------
// static method call
// ---------------------------

let text = """
module MyModule =
    type X<'b>() =
        static member Test<'a>(x:'a) = [|x|]

let z q = MyModule.X<bool>.Test<int>(q)
"""

let tast = translateText text


// ---------------------------
// method call
// ---------------------------

let text = """
module MyModule =
    type X() =
        member this.Test<'b> x = x
let x = MyModule.X()
let z = x.Test<string>(1)
"""

let tast = translateText text

let text = """
module MyModule =
    type X<'a>() = class end
let x = MyModule.X<int>()
"""

let tast = translateText text

let text = """
type HttpCookie(str:string) =
    member val Secure = false with get, set

let x= HttpCookie("c", Secure = true)
"""

let tast = translateText text
