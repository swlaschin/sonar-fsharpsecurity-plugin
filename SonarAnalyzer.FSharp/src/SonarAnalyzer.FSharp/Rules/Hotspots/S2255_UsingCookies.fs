module SonarAnalyzer.FSharp.Rules.S2255_UsingCookies

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open OptionBuilder
open EarlyReturn

// =================================================
// #2255 Writing cookies is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-2255
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S2255";
    let messageFormat = "Make sure that this cookie is written safely.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)


    let httpCookieClass = "HttpCookie"
    let iRequestCookieCollectionClass = "IRequestCookieCollection"
    let iResponseCookiesClass = "IResponseCookies"
    let nameValueCollectionClass = "NameValueCollection"
    let iDictionaryClass = "IDictionary"
    let setCookieString = "Set-Cookie"

    /// warn if set_Value is called on HttpCookie
    let checkHttpCookieSetValue (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch
            // is it a call to HttpCookie.set_Value?
            let! callCtx = callCtx |> CallExprHelper.tryMatchClassAndMethod httpCookieClass "set_Value"

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

    /// warn if HttpCookie is constructed with 2 args
    let checkHttpCookieCtor (ctx:TastContext) =
        option {
            // a class was created?
            let! newObjCtx = NewObjectExprHelper.tryMatch ctx

            // and it's HttpCookie?
            let! newObjCtx = newObjCtx |> NewObjectExprHelper.tryMatchClass httpCookieClass

            // and must be 2 args with second arg being the value to set
            if newObjCtx.Node.ArgTypes.Length < 2 then raise EarlyReturn

            return Diagnostic.Create(rule, newObjCtx.Node.Location)
            }

    /// warn if set_Item is called on HttpCookie
    let checkHttpCookieSetItem (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch
            // is it a call to HttpCookie.set_Item?
            let! callCtx = callCtx |> CallExprHelper.tryMatchClassAndMethod httpCookieClass "set_Item"

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

    /// warn if "Set-Cookie" is used as a key in set_Item
    let checkSetCookieAsKey (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch
            // is it a call to ANY "set_Item".
            // NOTE - the C# code is specific to certain classes, but using "Set-Cookie" anywhere should be flagged!
            let! callCtx = callCtx |> CallExprHelper.tryMatchMethod "set_Item"

            // and is the first arg "Set-Cookie"?
            let! arg0 = callCtx |> CallExprHelper.tryGetArgumentAt 0
            if arg0 |> ConstHelper.isEqualTo setCookieString |> not then raise EarlyReturn

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

    /// warn if set_Item is called on IRequestCookieCollection or IResponseCookies
    let checkCookieCollectionSetItem (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch
            let! declaringEntity = callCtx.Node.Member.DeclaringEntity

            // is it a call to any of the given classes?
            if [iRequestCookieCollectionClass; iResponseCookiesClass]
               |> List.contains declaringEntity.CompiledName
               |> not then raise EarlyReturn

            // is it a call to the setter?
            if callCtx.Node.Member.CompiledName <> "set_Item" then raise EarlyReturn

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

    /// warn if set_Item is called on NameValueCollection inside HttpCookie.Values
    let checkNameValueCollectionSetItem (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch

            // is it a call to NameValueCollection.set_Item?
            let! _ = callCtx |> CallExprHelper.tryMatchClassAndMethod nameValueCollectionClass "set_Item"

            // is the context inside a HttpCookie.get_Values call?
            let! parentCallCtx = ctx |> ContextHelper.tryCastAncestor<Tast.CallExpr>
            let! declaringEntity = callCtx.Node.Member.DeclaringEntity
            if httpCookieClass <> declaringEntity.CompiledName then raise EarlyReturn
            if parentCallCtx.Node.Member.CompiledName <> "get_Values" then raise EarlyReturn

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

    /// warn if Append is called on IResponseCookies
    let checkIResponseCookiesAppend (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch
            // is it a call to IResponseCookies.Append?
            let! callCtx = callCtx |> CallExprHelper.tryMatchClassAndMethod iResponseCookiesClass "Append"

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

    /// warn if IDictionary.Add is called with a "Set-Cookie" parameter
    let checkIDictionaryAdd (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch
            // is it a call to IDictionary.Add?
            let! callCtx = callCtx |> CallExprHelper.tryMatchClassAndMethod iDictionaryClass "Add"

            // and is the first arg "Set-Cookie"?
            let! arg0 = callCtx |> CallExprHelper.tryGetArgumentAt 0
            if arg0 |> ConstHelper.isEqualTo setCookieString |> not then raise EarlyReturn

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

    /// warn if NameObjectCollection.Add is called inside HttpCookie
    let checkNameValueCollectionAdd (ctx:TastContext) =
        option {
            // is it a call?
            let! callCtx = ctx |> CallExprHelper.tryMatch

            // is it a call to NameValueCollection.Add?
            let! _ = callCtx |> CallExprHelper.tryMatchClassAndMethod nameValueCollectionClass "Add"

            // is the context inside a HttpCookie.get_Values call?
            let! parentCallCtx = ctx |> ContextHelper.tryCastAncestor<Tast.CallExpr>
            let! _ = parentCallCtx |> CallExprHelper.tryMatchClassAndMethod httpCookieClass "get_Values"

            return Diagnostic.Create(rule, callCtx.Node.Location)
            }

open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    let (<|>) = EarlyReturn.orElse
    let rule =
        checkHttpCookieSetValue
        <|> checkHttpCookieCtor
        <|> checkHttpCookieSetItem
        <|> checkSetCookieAsKey
        <|> checkCookieCollectionSetItem
        <|> checkNameValueCollectionSetItem
        <|> checkIResponseCookiesAppend
        <|> checkIDictionaryAdd
        <|> checkNameValueCollectionAdd
    rule ctx

