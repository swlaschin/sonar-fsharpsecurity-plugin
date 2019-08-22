module rec SonarAnalyzer.FSharp.UnitTest.TestCases.S4792_ConfiguringLoggers_AspNetCore

open System
open System.Collections
open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Microsoft.AspNetCore

// hide the Deprecated warnings -- the code is copied from C#!
#nowarn "44"

//RSPEC S4792: https://jira.sonarsource.com/browse/RSPEC-4792
type ProgramLogging() =

    static member CreateWebHostBuilder(args:string[]) : IWebHostBuilder =
        WebHost.CreateDefaultBuilder(args) // Noncompliant {{Make sure that this logger's configuration is safe.}}
            .ConfigureLogging(fun loggingBuilder -> () )
            .UseStartup<StartupLogging>()


type StartupLogging() =
    member this.ConfigureServices(services:IServiceCollection ) =
        services.AddLogging(fun logging -> () ) // Noncompliant {{Make sure that this logger's configuration is safe.}}

    member this.Configure(app:IApplicationBuilder, env:IHostingEnvironment, loggerFactory:ILoggerFactory) =
        let config : IConfiguration = null
        let level = LogLevel.Critical
        let includeScopes = false
        let filter : Func<string, Microsoft.Extensions.Logging.LogLevel, bool> = null
        let consoleSettings : Microsoft.Extensions.Logging.Console.IConsoleLoggerSettings = null
        let azureSettings : Microsoft.Extensions.Logging.AzureAppServices.AzureAppServicesDiagnosticsSettings = null
        let eventLogSettings : Microsoft.Extensions.Logging.EventLog.EventLogSettings = null

        // An issue will be raised for each call to an ILoggerFactory extension methods adding loggers.
        loggerFactory.AddAzureWebAppDiagnostics() |> ignore  // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^    {{Make sure that this logger's configuration is safe.}}
        loggerFactory.AddAzureWebAppDiagnostics(azureSettings) |> ignore // Noncompliant
        loggerFactory.AddConsole() |> ignore // Noncompliant
        loggerFactory.AddConsole(level) |> ignore // Noncompliant
        loggerFactory.AddConsole(level, includeScopes)|> ignore  // Noncompliant
        loggerFactory.AddConsole(filter) |> ignore // Noncompliant
        loggerFactory.AddConsole(filter, includeScopes) |> ignore // Noncompliant
        loggerFactory.AddConsole(config) |> ignore // Noncompliant
        loggerFactory.AddConsole(consoleSettings) |> ignore // Noncompliant
        loggerFactory.AddDebug() |> ignore // Noncompliant
        loggerFactory.AddDebug(level) |> ignore // Noncompliant
        loggerFactory.AddDebug(filter) |> ignore // Noncompliant
        loggerFactory.AddEventLog() |> ignore // Noncompliant
        loggerFactory.AddEventLog(eventLogSettings) |> ignore // Noncompliant
        loggerFactory.AddEventLog(level) |> ignore // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^    {{Make sure that this logger's configuration is safe.}}

        // Testing the next method open a hack - see notes at the end of the file
        loggerFactory.AddEventSourceLogger() |> ignore // Noncompliant

        let providers : IEnumerable<ILoggerProvider> = null
        let filterOptions1 : LoggerFilterOptions = null
        let filterOptions2 : IOptionsMonitor<LoggerFilterOptions> = null

        use factory = new LoggerFactory() // Noncompliant
//                    ^^^^^^^^^^^^^^^^^^^    {{Make sure that this logger's configuration is safe.}}

        let p = new LoggerFactory(providers) // Noncompliant
        let p = new LoggerFactory(providers, filterOptions1) // Noncompliant
        let p = new LoggerFactory(providers, filterOptions2) // Noncompliant
        ()


    member this.AdditionalTests(webHostBuilder:IWebHostBuilder,  serviceDescriptors:IServiceCollection) =
        let factory = new MyLoggerFactory()  // Noncompliant
//                    ^^^^^^^^^^^^^^^^^^^^^
        let l = new MyLoggerFactory("data") // Noncompliant
        let loggingBuilder :Action<ILoggingBuilder> = null

        // Calling extension methods as static methods
        WebHostBuilderExtensions.ConfigureLogging(webHostBuilder, loggingBuilder) |> ignore           // Noncompliant
        LoggingServiceCollectionExtensions.AddLogging(serviceDescriptors, loggingBuilder) |> ignore    // Noncompliant

        AzureAppServicesLoggerFactoryExtensions.AddAzureWebAppDiagnostics(factory, null) |> ignore       // Noncompliant
        ConsoleLoggerExtensions.AddConsole(factory) |> ignore                                // Noncompliant
        DebugLoggerFactoryExtensions.AddDebug(factory) |> ignore                             // Noncompliant
        EventLoggerFactoryExtensions.AddEventLog(factory) |> ignore                          // Noncompliant
        EventSourceLoggerFactoryExtensions.AddEventSourceLogger(factory) |> ignore           // Noncompliant

type MyLoggerFactory(data:string) =

    new() = new MyLoggerFactory("")

    interface ILoggerFactory with
        member this.AddProvider(provider:ILoggerProvider) = ()
        member this.CreateLogger(categoryName:string ) : ILogger = null
        member this.Dispose() = ()


