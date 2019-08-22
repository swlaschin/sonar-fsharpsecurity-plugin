module SonarAnalyzer.FSharp.UnitTest.TestCases.S2092_CookieShouldBeSecure

open System

type HttpCookie(str:string) =
    member val Secure = false with get, set
    member val HttpOnly = false with get, set

type Program() =

    let mutable field1 = HttpCookie("c") // Noncompliant
    let mutable field2 = None

    member val Property1 = HttpCookie("c") with get, set // Noncompliant
    member val Property2 = None with get, set

    member this.CtorSetsAllowedValue() =
        // none
        ()

    member this.CtorSetsNotAllowedValue() =
        HttpCookie("c") |> ignore // Noncompliant {{Make sure creating this cookie without setting the 'Secure' property is safe here.}}

    member this.InitializerSetsAllowedValue() =
        HttpCookie("c", Secure = true) |> ignore

    member this.InitializerSetsNotAllowedValue() =
        HttpCookie("c", Secure = false) |> ignore // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        HttpCookie("c") |> ignore // Noncompliant
        HttpCookie("c", HttpOnly = true) |> ignore // Noncompliant

    member this.PropertySetsNotAllowedValue() =
        let c = new HttpCookie("c", Secure = true)
        c.Secure <- false // Noncompliant
//      ^^^^^^^^^^^^^^^^

        field1.Secure <- false // Noncompliant
        //this.field1.Secure <- false // Noncompliant

        //Property1.Secure <- false // Noncompliant
        this.Property1.Secure <- false // Noncompliant

    member this.PropertySetsAllowedValue(foo:bool) =
        let c1 = HttpCookie("c") // Compliant, Secure is set below
        c1.Secure <- true

        field1 <- HttpCookie("c") // Compliant, Secure is set below
        field1.Secure <- true

        field2 <- Some (HttpCookie("c")) // Compliant, Secure is set below
        field2.Value.Secure <- true

        this.Property1 <- HttpCookie("c") // Compliant, Secure is set below
        this.Property1.Secure <- true

        this.Property2 <- Some (HttpCookie("c")) // Compliant, Secure is set below
        this.Property2.Value.Secure <- true

        //let c2 = HttpCookie("c") // Noncompliant, Secure is set conditionally
        //if foo then
        //    c2.Secure <- true

        let c3 = HttpCookie("c") // Compliant, Secure is set after the if
        if foo then
            // do something
            ()
        c3.Secure <- true

        let mutable c4 : HttpCookie = Unchecked.defaultof<HttpCookie>
        //if foo then
        //    c4 <- HttpCookie("c") // Noncompliant, Secure is not set in the same scope
        c4.Secure <- true
