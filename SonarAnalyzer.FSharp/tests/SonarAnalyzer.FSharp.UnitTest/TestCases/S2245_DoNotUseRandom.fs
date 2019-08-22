module SonarAnalyzer.FSharp.UnitTest.TestCases.S2245_DoNotUseRandom

open System
open System.Security.Cryptography

module Main =

    let r1 = Random() // Noncompliant {{Make sure that using this pseudorandom number generator is safe here.}}
//          ^^^^^^^^^^^^
    let r2 = Random(1) // Noncompliant

    let r3 = EventArgs() // Compliant, not Random

    let r4 = RandomNumberGenerator.Create() // Compliant, using cryptographically strong RNG

    ()
