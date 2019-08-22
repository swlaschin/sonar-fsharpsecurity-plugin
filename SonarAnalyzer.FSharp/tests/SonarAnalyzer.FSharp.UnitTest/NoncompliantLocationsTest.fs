module SonarAnalyzer.FSharp.UnitTest.NoncompliantLocationsTest

open NUnit.Framework

// check that the NonCompliantLocations logic is working

[<Test>]
let ``check that noncompliant location parsing works correctly``() =
    let lines = [
        @"1. // Compliant"
        @"2. normal code // Noncompliant"
        @"2. // commented out code // Noncompliant"
        @"3. Noncompliant() // a call not a comment"
    ]
    let actual = Verifier.getNoncompliantLocations lines |> sprintf "%A"
    let expected = [2] |> sprintf "%A"
    Assert.AreEqual(expected,actual)


