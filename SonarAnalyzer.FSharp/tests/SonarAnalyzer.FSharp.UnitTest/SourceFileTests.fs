module SonarAnalyzer.FSharp.UnitTest.SourceFileTests

open System
open NUnit.Framework
open SonarAnalyzer.FSharp

[<Test>]
let S1313_HardcodedIpAddress() =
    let rule = Rules.S1313_HardcodedIpAddress.Rule
    Verifier.verify @"TestCases/S1313_HardcodedIpAddress.fs" rule

[<Test>]
let S2077_ExecutingSqlQueries() =
    let rule = Rules.S2077_ExecutingSqlQueries.Rule
    Verifier.verify @"TestCases/S2077_ExecutingSqlQueries.fs" rule

[<Test>]
let S2092_CookieShouldBeSecure() =
    let rule = Rules.S2092_CookieShouldBeSecure.Rule
    Verifier.verify @"TestCases/S2092_CookieShouldBeSecure.fs" rule

[<Test>]
let S2245_DoNotUseRandom() =
    let rule = Rules.S2245_DoNotUseRandom.Rule
    Verifier.verify @"TestCases/S2245_DoNotUseRandom.fs" rule

[<Test>]
[<Ignore("not implemented")>]//Requires NuGet
let S2255_UsingCookies() =
    let rule = Rules.S2255_UsingCookies.Rule
    Verifier.verify @"TestCases/S2255_UsingCookies.fs" rule

[<Test>]
let S3011_BypassingAccessibility() =
    let rule = Rules.S3011_BypassingAccessibility.Rule
    Verifier.verify @"TestCases/S3011_BypassingAccessibility.fs" rule

[<Test>]
[<Ignore("not implemented")>]//Requires NuGet
let S4507_DeliveringDebugFeaturesInProduction() =
    let rule = Rules.S4507_DeliveringDebugFeaturesInProduction.Rule
    Verifier.verify @"TestCases/S4507_DeliveringDebugFeaturesInProduction.fs" rule

[<Test>]
let S4784_UsingRegularExpressions() =
    let rule = Rules.S4784_UsingRegularExpressions.Rule
    Verifier.verify @"TestCases/S4784_UsingRegularExpressions.fs" rule

[<Test>]
[<Ignore("not implemented")>]
let S4787_EncryptingData() =
    let rule = Rules.S4787_EncryptingData.Rule
    Verifier.verify @"TestCases/S4787_EncryptingData.fs" rule

[<Test>]
[<Ignore("not implemented")>]
let S4790_CreatingHashAlgorithms() =
    let rule = Rules.S4790_CreatingHashAlgorithms.Rule
    Verifier.verify @"TestCases/S4790_CreatingHashAlgorithms.fs" rule

[<Test>]
[<Ignore("not implemented")>]//Requires NuGet
let S4792_ConfiguringLoggers_AspNetCore() =
    let rule = Rules.S4792_ConfiguringLoggers.Rule
    Verifier.verify @"TestCases/S4792_ConfiguringLoggers_AspNetCore.fs" rule

[<Test>]
[<Ignore("not implemented")>]//Requires NuGet
let S4792_ConfiguringLoggers_Serilog() =
    let rule = Rules.S4792_ConfiguringLoggers.Rule
    Verifier.verify @"TestCases/S4792_ConfiguringLoggers_Serilog.fs" rule

[<Test>]
[<Ignore("not implemented")>]
let S4818_SocketsCreation() =
    let rule = Rules.S4818_SocketsCreation.Rule
    Verifier.verify @"TestCases/S4818_SocketsCreation.fs" rule

[<Test>]
[<Ignore("not implemented")>]
let S4823_UsingCommandLineArguments() =
    let rule = Rules.S4823_UsingCommandLineArguments.Rule
    Verifier.verify @"TestCases/S4823_UsingCommandLineArguments.fs" rule

[<Test>]
[<Ignore("not implemented")>]
let S4829_ReadingStandardInput() =
    let rule = Rules.S4829_ReadingStandardInput.Rule
    Verifier.verify @"TestCases/S4829_ReadingStandardInput.fs" rule

[<Test>]
[<Ignore("not implemented")>]//Requires NuGet
let S4834_ControllingPermissions() =
    let rule = Rules.S4834_ControllingPermissions.Rule
    Verifier.verify @"TestCases/S4834_ControllingPermissions.fs" rule

[<Test>]
[<Ignore("not implemented")>]
let S5042_ExpandingArchiveFiles() =
    let rule = Rules.S5042_ExpandingArchiveFiles.Rule
    Verifier.verify @"TestCases/S5042_ExpandingArchiveFiles.fs" rule

