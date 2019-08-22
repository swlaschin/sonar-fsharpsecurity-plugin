module SonarAnalyzer.FSharp.UnitTest.RspecStringsTest

open SonarAnalyzer.FSharp
open NUnit.Framework

// check that the RspecStrings resources has been embedded and access logic is working

[<Test>]
let getStringSucceeds() =
    let rm = RspecStrings.ResourceManager
    let str = rm.GetString("S1313_Title")
    Assert.IsNotNull(str)
