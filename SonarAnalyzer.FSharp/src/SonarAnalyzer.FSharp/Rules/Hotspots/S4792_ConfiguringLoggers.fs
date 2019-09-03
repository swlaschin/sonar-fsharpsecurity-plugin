module SonarAnalyzer.FSharp.Rules.S4792_ConfiguringLoggers

open SonarAnalyzer.FSharp
open SonarAnalyzer.FSharp.RuleHelpers
open FSharpAst
open System.Net

// =================================================
// #4792 Configuring loggers is security-sensitive
// https://rules.sonarsource.com/csharp/type/Security%20Hotspot/RSPEC-4792
// =================================================

module Private =

    [<Literal>]
    let DiagnosticId = "S4792";
    let messageFormat = "Make sure that this logger's configuration is safe.";
    let rule = DiagnosticDescriptor.Create(DiagnosticId, messageFormat, RspecStrings.ResourceManager)

    exception EarlyReturn

    let checkWithEarlyReturn f x =
        try
            f x
        with
        | :? EarlyReturn ->
            None

    /// Return the location if the method matches the context, otherwise None
    let matchMethod className methodName (ctx:TastContext) =
        let call, declaringEntity =
            option {
                let! call = ctx.Try<Tast.CallExpr>()
                let! declaringEntity = call.Member.DeclaringEntity
                return call,declaringEntity
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        let entityPath = sprintf "%s.%s" declaringEntity.AccessPath declaringEntity.DisplayName
        if entityPath = className && call.Member.DisplayName = methodName then
            Some call.Location
        else
            None

    /// Return the location if the interface is implemented in the context
    let matchImplements _interfaceName (ctx:TastContext) =
        let _declaringEntity =
            option {
                let! call = ctx.Try<Tast.NewObjectExpr>()
                let! declaringEntity = call.Ctor.DeclaringEntity
                return declaringEntity
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // todo
        None

    /// True if the property setter is called
    let matchPropertySetter className propertyName (ctx:TastContext) =
        let methodName = "set_" + propertyName
        matchMethod className methodName ctx

    /// True if the new object derives from the specified base class
    let matchDerivesFrom _baseclassName (ctx:TastContext) =
        let _declaringEntity =
            option {
                let! call = ctx.Try<Tast.NewObjectExpr>()
                let! declaringEntity = call.Ctor.DeclaringEntity
                return declaringEntity
                }
            |> Option.defaultWith (fun _ -> raise EarlyReturn)

        // todo
        None

    let allChecks = [
        // ASP.NET Core
        matchMethod "Microsoft.AspNetCore.Hosting.WebHostBuilderExtensions" "ConfigureLogging"
        matchMethod "Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions" "AddLogging"
        matchMethod "Microsoft.Extensions.Logging.ConsoleLoggerExtensions" "AddConsole"
        matchMethod "Microsoft.Extensions.Logging.DebugLoggerFactoryExtensions" "AddDebug"
        matchMethod "Microsoft.Extensions.Logging.EventLoggerFactoryExtensions" "AddEventLog"
        matchMethod "Microsoft.Extensions.Logging.EventLoggerFactoryExtensions" "AddEventSourceLogger"
        matchMethod "Microsoft.Extensions.Logging.EventSourceLoggerFactoryExtensions" "AddEventSourceLogger"
        matchMethod "Microsoft.Extensions.Logging.AzureAppServicesLoggerFactoryExtensions" "AddAzureWebAppDiagnostics"
        matchImplements "Microsoft.Extensions.Logging.ILoggerFactory"

        // log4net
        matchMethod "log4net.Config.XmlConfigurator" "Configure"
        matchMethod "log4net.Config.XmlConfigurator" "ConfigureAndWatch"
        matchMethod "log4net.Config.DOMConfigurator" "Configure"
        matchMethod "log4net.Config.DOMConfigurator" "ConfigureAndWatch"
        matchMethod "log4net.Config.BasicConfigurator" "Configure"
        matchMethod "log4net.Config.BasicConfigurator" "ConfigureAndWatch"
        matchMethod "log4net.Config.BasicConfigurator" "Configure"
        matchMethod "log4net.Config.BasicConfigurator" "ConfigureAndWatch"

        // NLog
        matchPropertySetter "NLog.LogManager" "Configuration"

        // Serilog
        matchDerivesFrom "Serilog.LoggerConfiguration"

        ]

    /// Do all the checks
    let runChecks (ctx:TastContext) =
        // if any check matches the context, fire the rule
        allChecks
        |> List.tryPick (fun check -> check ctx)
        |> Option.map (fun callLocation -> Diagnostic.Create(rule, callLocation, [||]) )


open Private

/// The implementation of the rule
[<Rule(DiagnosticId)>]
let Rule : Rule = fun ctx ->
    checkWithEarlyReturn runChecks ctx
