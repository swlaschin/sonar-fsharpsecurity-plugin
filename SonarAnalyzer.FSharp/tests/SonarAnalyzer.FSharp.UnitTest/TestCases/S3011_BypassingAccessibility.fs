module SonarAnalyzer.FSharp.UnitTest.TestCases.S3011_BypassingAccessibility

open System
open System.Reflection

module Program =

    let test() =
        // RSPEC: https://jira.sonarsource.com/browse/RSPEC-3011
        let dynClass = Type.GetType("MyInternalClass")
        // Questionable. Using BindingFlags.NonPublic will return non-public members
        let bindingAttr = BindingFlags.NonPublic ||| BindingFlags.Static  // Noncompliant
//                        ^^^^^^^^^^^^^^^^^^^^^^   {{Make sure that this accessibility bypass is safe here.}}
        let dynMethod = dynClass.GetMethod("mymethod", bindingAttr)
        let result = dynMethod.Invoke(dynClass, null)

        ()


    let additionalChecks(t:System.Type) : BindingFlags =
        // Using other binding attributes should be ok
        let bindingAttr =
            BindingFlags.Static ||| BindingFlags.CreateInstance ||| BindingFlags.DeclaredOnly |||
            BindingFlags.ExactBinding ||| BindingFlags.GetField ||| BindingFlags.InvokeMethod // et cetera...
        let dynMeth = t.GetMember("mymethod", bindingAttr)

        // We don't detect casts to the forbidden value
        let nonPublic : BindingFlags = enum 32
        let dynMeth = t.GetMember("mymethod", nonPublic)

        let v = Enum.TryParse<BindingFlags>("NonPublic")
        let dynMeth = t.GetMember("mymethod", nonPublic)

        let bindingAttr = (((BindingFlags.NonPublic)) ||| BindingFlags.Static) // Noncompliant
//                           ^^^^^^^^^^^^^^^^^^^^^^
        let dynMeth = t.GetMember("mymethod", (BindingFlags.NonPublic)) // Noncompliant
//                                             ^^^^^^^^^^^^^^^^^^^^^^
        let v = (int)BindingFlags.NonPublic // Noncompliant
        BindingFlags.NonPublic  // Noncompliant

    let defaultAccess = BindingFlags.OptionalParamBinding ||| BindingFlags.NonPublic // Noncompliant
//                                                            ^^^^^^^^^^^^^^^^^^^^^^

    let private access1 = BindingFlags.NonPublic     // Noncompliant

    let access2 = BindingFlags.NonPublic      // Noncompliant
    let getBindingFlags() = BindingFlags.NonPublic    // Noncompliant

