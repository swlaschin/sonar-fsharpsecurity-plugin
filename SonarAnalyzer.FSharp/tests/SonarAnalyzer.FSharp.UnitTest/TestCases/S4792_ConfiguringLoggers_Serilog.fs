module rec SonarAnalyzer.FSharp.UnitTest.TestCases.S4792_ConfiguringLoggers_Serilog

open Serilog
open Serilog.Core


type SerilogLogging() =

    // RSPEC-4792: https://jira.sonarsource.com/browse/RSPEC-4792
    member this.Foo() =
        new Serilog.LoggerConfiguration()  // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^   {{Make sure that this logger's configuration is safe.}}

    member this.AdditionalTests() =
        let config = LoggerConfiguration() // Noncompliant
        let config = MyConfiguration()         // Noncompliant

        // Using the logger shouldn't raise issues
        let levelSwitch = new LoggingLevelSwitch()
        levelSwitch.MinimumLevel <- Serilog.Events.LogEventLevel.Warning

        let newLog =
            config.MinimumLevel.ControlledBy(levelSwitch)
              .WriteTo.Console()
              .CreateLogger()

        Log.Logger <- newLog
        Log.Information("logged info")
        Log.CloseAndFlush()

type MyConfiguration() =
    inherit LoggerConfiguration()
