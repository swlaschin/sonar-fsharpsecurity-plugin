module SonarAnalyzer.FSharp.UnitTest.TestCases.S1313_HardcodedIpAddress

/// Dummy class for testing
type AnyAssemblyClass(s:string) = class end

/// Dummy class for testing
type SomeAttribute(s:string) =
    inherit System.Attribute()

/// Dummy function for testing
let writeAssemblyInfo(assemblyName:string, version:string, author:string, description:string, title:string) =
    ()


[<SomeAttribute("127.0.0.1")>] // this is mainly for assembly versions
let hardcodedIpAddress() =

    let ip1 = "192.168.0.1" // Noncompliant {{Make sure using this hardcoded IP address '192.168.0.1' is safe here.}}
//            ^^^^^^^^^^^^^

    let ip2 = "300.0.0.0" // Compliant, not a valid IP
    let ip3 = "127.0.0.1" // Compliant, this is an exception in the rule (see: https://github.com/SonarSource/sonar-csharp/issues/1540)
    let ip4 = "    127.0.0.0    " // Compliant
    let ip5 = @"    ""127.0.0.0""    " // Compliant

    let ip6 = "2001:db8:1234:ffff:ffff:ffff:ffff:ffff" // Noncompliant
    let ip7 = "::/0" // Compliant, not recognized as IPv6 address
    let ip8 = "::" // Compliant, this is an exception in the rule

    let ip9 = "2" // Compliant, should not be recognized as 0.0.0.2

    let v = System.Version("127.0.0.0") //Compliant
    let a = AnyAssemblyClass("127.0.0.0") //Compliant

    //Compliant
    writeAssemblyInfo("Project","1.2.0.0","Thomas","Content","Package")
