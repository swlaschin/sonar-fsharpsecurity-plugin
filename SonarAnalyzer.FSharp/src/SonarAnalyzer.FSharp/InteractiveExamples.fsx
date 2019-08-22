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
#r @"bin\Debug\netstandard2.0\SonarAnalyzer.FSharp.dll"
#r @"netstandard"

open FSharpAst
open SonarAnalyzer.FSharp
open System.Reflection

/// create a dummy context to run each rule on
let dummyNode : Tast.ImplementationFile = {Name= "dummy"; Decls=[]}
let ctx : TastContext = {Filename=dummyNode.Name; Node=dummyNode; Ancestors=[]}

/// Return all the assemblies to analyze
let assemblies() : Assembly list =
    [
    typeof<RuleAttribute>.Assembly
    ]

let ruleS2092Method =
    let t = typeof<SonarAnalyzer.FSharp.Rules.S2092_CookieShouldBeSecure.Private.EarlyReturn>
    let t = t.DeclaringType
    let t = t.DeclaringType
    let ruleMethod = t.GetMethods().[0]
    ruleMethod

let ruleS2092 : Rule =
    fun ctx -> box (ruleS2092Method.Invoke(null,[|ctx|])) :?> Diagnostic option

// invoke
ruleS2092 ctx

let allRuleMethods =
    assemblies()
    |> Seq.collect (fun assembly -> assembly.GetTypes())
    |> Seq.collect (fun ty -> ty.GetMethods() )
    |> Seq.filter (fun m -> m.GetCustomAttributes<RuleAttribute>() |> Seq.isEmpty |> not )
    |> Seq.toList

let allRules =
    allRuleMethods
    |> List.map (fun m ->
        let r: Rule = fun ctx -> m.Invoke(null,[|ctx|]) :?> Diagnostic option
        let a:RuleAttribute = m.GetCustomAttributes<RuleAttribute>() |> Seq.head
        a.Key,r
        )

