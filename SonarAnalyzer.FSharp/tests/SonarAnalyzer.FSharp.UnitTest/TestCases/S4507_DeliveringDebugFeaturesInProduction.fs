module SonarAnalyzer.FSharp.UnitTest.TestCases.S4507_DeliveringDebugFeaturesInProduction

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

module Startup =

    let Configure(app:IApplicationBuilder, env:IHostingEnvironment) =
        // Invoking as extension methods
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore // Compliant
            app.UseDatabaseErrorPage() |> ignore  // Compliant

        // Invoking as static methods
        if (HostingEnvironmentExtensions.IsDevelopment(env)) then
            DeveloperExceptionPageExtensions.UseDeveloperExceptionPage(app) |> ignore // Compliant
            DatabaseErrorPageExtensions.UseDatabaseErrorPage(app) |> ignore // Compliant

        // Not in development
        if not (env.IsDevelopment()) then
            DeveloperExceptionPageExtensions.UseDeveloperExceptionPage(app) |> ignore // Noncompliant
            DatabaseErrorPageExtensions.UseDatabaseErrorPage(app) |> ignore // Noncompliant

        // Custom conditions are deliberately ignored
        let isDevelopment = env.IsDevelopment()
        if (isDevelopment) then
            app.UseDeveloperExceptionPage() |> ignore // Noncompliant, False Positive
            app.UseDatabaseErrorPage() |> ignore // Noncompliant, False Positive

        // These are called unconditionally
        app.UseDeveloperExceptionPage() |> ignore // Noncompliant
        app.UseDatabaseErrorPage() |> ignore // Noncompliant
        DeveloperExceptionPageExtensions.UseDeveloperExceptionPage(app) |> ignore // Noncompliant
        DatabaseErrorPageExtensions.UseDatabaseErrorPage(app) // Noncompliant
