module SonarAnalyzer.FSharp.UnitTest.TestCases.S2255_UsingCookies

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

module Program =

    let responses(response:HttpResponse ) =

        // Response headers
        response.Headers.Add("Set-Cookie", StringValues "") // Noncompliant
        response.Headers.["Set-Cookie"] <- StringValues "" // Noncompliant
        let value = response.Headers.["Set-Cookie"] // Compliant

        // Not the Set-Cookie header
        response.Headers.Add("something", StringValues "")
        response.Headers.["something"] <- value
        let value = response.Headers.["something"]

        // Response headers as variable
        let responseHeaders = response.Headers
        responseHeaders.Add("Set-Cookie", StringValues "") // Noncompliant
        responseHeaders.["Set-Cookie"] <- StringValues "" // Noncompliant
        let value = responseHeaders.["Set-Cookie"] // Compliant

        responseHeaders.Remove("Set-Cookie") |> ignore // Compliant
        responseHeaders.Remove("") |> ignore // Compliant

        // Response cookies as property
        response.Cookies.Append("", "") // Noncompliant
        response.Cookies.Append("", "", CookieOptions() ) // Noncompliant

        // Response cookies as variable
        let responseCookies = response.Cookies
        responseCookies.Append("", "") // Noncompliant
        responseCookies.Append("", "", CookieOptions() ) // Noncompliant

        responseCookies.Delete("") // Compliant

    let requests(request:HttpRequest )=

        let value = StringValues ""

        // Request headers
        request.Headers.Add("Set-Cookie", StringValues "") // Noncompliant
        request.Headers.["Set-Cookie"] <- value // Noncompliant
        let value = request.Headers.["Set-Cookie"] // Compliant

        // Not the Set-Cookie header
        request.Headers.Add("something", StringValues "")
        request.Headers.["something"] <- value
        let value = request.Headers.["something"]

        // Request headers as variable
        let requestHeaders = request.Headers
        requestHeaders.Add("Set-Cookie", StringValues "") // Noncompliant
        requestHeaders.["Set-Cookie"] <- value // Noncompliant
        let value = requestHeaders.["Set-Cookie"] // Compliant

        requestHeaders.Remove("Set-Cookie") |> ignore // Compliant
        requestHeaders.Remove("") |> ignore // Compliant

        // Request cookies as property
        let value = request.Cookies.[""] // Compliant
        let v = request.Cookies.TryGetValue("") // Compliant

        // Request cookies as variable
        let requestCookies = request.Cookies
        let value = requestCookies.[""] // Compliant
        let v = requestCookies.TryGetValue("") // Compliant

        ()
