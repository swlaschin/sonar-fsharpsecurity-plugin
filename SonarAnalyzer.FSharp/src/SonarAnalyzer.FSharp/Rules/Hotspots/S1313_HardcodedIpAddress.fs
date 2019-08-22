module SonarAnalyzer.FSharp.Rules.S1313_HardcodedIpAddress

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #1313 Using hardcoded IP addresses is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-1313
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S1313"
    let messageFormat = "Make sure using this hardcoded IP address '{0}' is safe here."
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

    let checkWithEarlyReturn f node =
        try
            f node
        with
        | :? EarlyReturn ->
            None


    let parseIpAddress str =
        match System.Net.IPAddress.TryParse(str) with
        | false,_ ->
            None
        | true, address ->
            Some address

    let isIp4Address (address:IPAddress) =
        address.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork

    let isValidIpAddress literalValue =
        option {
            let! address = parseIpAddress literalValue
            if (isIp4Address address) &&
                // must have 4 parts
                literalValue.Split('.').Length <> 4 then
                return false
            else
                return true
            // if address parsing fails, return false
            } |> Option.defaultValue false


    let ignoredNames = ["VERSION"; "ASSEMBLY"]

    let isIgnoredMemberName (ctx:TastContext) =
        option {
            // if there is a containing call, get it, else drop through and return false later
            let! _,node = tryContainingCall ctx
            let isContainedInMember name =
                node.Member.CompiledName.ToUpperInvariant().Contains(name)
            // if any matches found, return true
            return ignoredNames |> List.exists isContainedInMember
        } |> Option.defaultValue false

    let isIgnoredClassName (ctx:TastContext) =
        option {
            // if there is a containing call, get it, else drop through and return false later
            let! _,node = tryContainingNewObjectExpr ctx
            let! entity = node.Ctor.DeclaringEntity
            let isContainedInClassName name =
                entity.CompiledName.ToUpperInvariant().Contains(name)
            // if any matches found, return true
            return ignoredNames |> List.exists isContainedInClassName
        } |> Option.defaultValue false


    let checkNode (ctx:TastContext) (node:Tast.ConstantExpr) =
        if node.Type |> isType WellKnownType.string |> not then raise EarlyReturn

        let literalValue = (string node.Value) // .Trim() // null is converted to "" by `string` so this is safe
        // Note Trim() is not used in the C# version

        let allowedValues = [""; "::"; "127.0.0.1"]
        if allowedValues |> List.contains literalValue then raise EarlyReturn

        if not (isValidIpAddress literalValue) then raise EarlyReturn

        // check for ignored names such as "ASSEMBLY"
        if isIgnoredClassName ctx then raise EarlyReturn
        if isIgnoredMemberName ctx then raise EarlyReturn

        // OK if used in an attribute
        // NB attributes are not normally visited in the AST. Must be asked for explicitly.

        Diagnostic.Create(rule, node.Location, literalValue) |> Some

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->

    // only trigger for constants
    ctx.Try<Tast.ConstantExpr>()
    |> Option.bind (checkWithEarlyReturn (checkNode ctx))


