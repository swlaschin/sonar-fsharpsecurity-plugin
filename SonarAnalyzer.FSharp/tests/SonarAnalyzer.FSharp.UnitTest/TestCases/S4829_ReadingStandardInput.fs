module SonarAnalyzer.FSharp.UnitTest.TestCases.S4829_ReadingStandardInput

open System
type Con = System.Console

type MyConsole() =
    static member Read() = 1
    static member ReadKey() = 1
    static member In =
        new System.IO.StringReader("") :> System.IO.TextReader

type Program() =

    member this.Method() =
        let code = System.Console.Read() // Noncompliant {{Make sure that reading the standard input is safe here.}}
//                 ^^^^^^^^^^^^^^^^^^^^^
        let code = Con.Read() // Noncompliant

        let value = Console.ReadLine() // Noncompliant
        let code = Console.Read() // Noncompliant
        let key = Console.ReadKey() // Noncompliant
        let key = Console.ReadKey(true) // Noncompliant

        Console.Read() |> ignore // Compliant, value is ignored
        Console.ReadLine() |> ignore // Compliant, value is ignored
        Console.ReadKey() |> ignore // Compliant, value is ignored
        Console.ReadKey(true) |> ignore // Compliant, value is ignored

        Console.OpenStandardInput() |> ignore // Noncompliant
        Console.OpenStandardInput(100) |> ignore // Noncompliant

        let x = System.Console.In // Noncompliant
//              ^^^^^^^^^^^^^^^^^
        let x = Console.In // Noncompliant
        let x = Con.In // Noncompliant
        Console.In.Read() |> ignore // Noncompliant

        // Other Console methods
        Console.Write(1)
        Console.WriteLine(1)
        // Other classes
        MyConsole.Read() |> ignore
        MyConsole.In.Read() |> ignore
        ()


