module SonarAnalyzer.FSharp.UnitTest.TestCases.S4823_UsingCommandLineArguments

open System

type Program1() =

    static member Main([<ParamArray>] args:string[]) = // Noncompliant {{Make sure that command line arguments are used safely here.}}
//                                    ^^^^
        Console.WriteLine(args.[0])

type Program2() =

    static member Main([<ParamArray>] args:string[]) = // Compliant, args is not used
        ()

    static member Main(arg:string ) = // Compliant, doesn't conform to signature for a Main method
        Console.WriteLine(arg)

    static member Main(x:int, [<ParamArray>] args:string[]) = // Compliant, doesn't conform to signature for a Main method
        Console.WriteLine(args)

type Program3() =
    static let staticArgs : string[] = [||]

    static member Main([<ParamArray>] args:string[]) = // Compliant, args is not used
        Console.WriteLine(staticArgs)

type Program4() =
    static member Main([<ParamArray>] args:string[]) : string = // Compliant, doesn't conform to signature for a Main method
        Console.WriteLine(args)
        ""
