module SonarAnalyzer.FSharp.UnitTest.TestCases.S4784_UsingRegularExpressions

open System
open System.Text.RegularExpressions
type RE = System.Text.RegularExpressions.Regex


module Program =

    let main(s:string) =
        let r = new Regex("") // Compliant, less than 3 characters
        let r = new Regex("**") // Compliant, less than 3 characters
        let r = new Regex("+*") // Compliant, less than 3 characters
        let r = new Regex("abcdefghijklmnopqrst") // Compliant, does not have the special characters
        let r = new Regex("abcdefghijklmnopqrst+") // Compliant, has only 1 special character
        let r = new Regex("{abc}+defghijklmnopqrst") // Noncompliant
        let r = new Regex("{abc}+{a}") // Noncompliant {{Make sure that using a regular expression is safe here.}}
//              ^^^^^^^^^^^^^^^^^^^^^^
        let r = new Regex("+++") // Noncompliant
        let r = new Regex(@"\+\+\+") // Noncompliant FP (escaped special characters)
        let r = new Regex("{{{") // Noncompliant
        let r = new Regex(@"\{\{\{") // Noncompliant FP (escaped special characters)
        let r = new Regex("***") // Noncompliant
        let r = new Regex(@"\*\*\*") // Noncompliant FP (escaped special characters)
        let r = new Regex("(a+)+s", RegexOptions.Compiled) // Noncompliant
        let r = new Regex("(a+)+s", RegexOptions.Compiled, TimeSpan.Zero) // Noncompliant
        let r = new Regex("{ab}*{ab}+{cd}+foo*") // Noncompliant

        Regex.IsMatch("", "(a+)+s") |> ignore // Noncompliant
//      ^^^^^^^^^^^^^^^^^^^^^^^^^^^
        Regex.IsMatch(s, "(a+)+s", RegexOptions.Compiled) |> ignore // Noncompliant
        Regex.IsMatch("", "{foo}{bar}", RegexOptions.Compiled, TimeSpan.Zero) |> ignore // Noncompliant

        Regex.Match(s, "{foo}{bar}") |> ignore // Noncompliant
        Regex.Match("", "{foo}{bar}", RegexOptions.Compiled) |> ignore // Noncompliant
        Regex.Match("", "{foo}{bar}", RegexOptions.Compiled, TimeSpan.Zero) |> ignore // Noncompliant

        Regex.Matches(s, "{foo}{bar}") |> ignore // Noncompliant
        Regex.Matches("", "{foo}{bar}", RegexOptions.Compiled) |> ignore // Noncompliant
        Regex.Matches("", "{foo}{bar}", RegexOptions.Compiled, TimeSpan.Zero) |> ignore  // Noncompliant

        Regex.Replace(s, "ab*cd*", fun m -> "") |> ignore // Noncompliant
        Regex.Replace("", "ab*cd*", "") |> ignore // Noncompliant
        Regex.Replace("", "ab*cd*", MatchEvaluator (fun m -> ""), RegexOptions.Compiled) |> ignore // Noncompliant
        Regex.Replace("", "ab*cd*", s, RegexOptions.Compiled) |> ignore // Noncompliant
        Regex.Replace("", "ab*cd*", MatchEvaluator (fun m -> ""), RegexOptions.Compiled, TimeSpan.Zero) |> ignore // Noncompliant
        Regex.Replace("", "ab*cd*", "", RegexOptions.Compiled, TimeSpan.Zero) |> ignore // Noncompliant
        Regex.Replace("", "ab\\*cd\\*", "", RegexOptions.Compiled, TimeSpan.Zero) |> ignore // Noncompliant FP (escaped special characters)

        Regex.Split("", "a+a+") |> ignore // Noncompliant
        Regex.Split("", "a+a+", RegexOptions.Compiled) |> ignore // Noncompliant
        Regex.Split("", "a+a+", RegexOptions.Compiled, TimeSpan.Zero) |> ignore // Noncompliant

        new System.Text.RegularExpressions.Regex("a+a+") |> ignore // Noncompliant
        new RE("a+b+") |> ignore // Noncompliant
        System.Text.RegularExpressions.Regex.IsMatch("", "{}{}") |> ignore // Noncompliant
        RE.IsMatch("", "a**") |> ignore // Noncompliant
        // IsMatch("", "b**") |> ignore // Noncompliant

        // Non-static methods are compliant
        r.IsMatch("a+a+") |> ignore
        r.IsMatch("{ab}*{ab}+{cd}+foo*", 0) |> ignore

        r.Match("{ab}*{ab}+{cd}+foo*") |> ignore
        r.Match("{ab}*{ab}+{cd}+foo*", 0) |> ignore
        r.Match("{ab}*{ab}+{cd}+foo*", 0, 1) |> ignore

        r.Matches("{ab}*{ab}+{cd}+foo*") |> ignore
        r.Matches("{ab}*{ab}+{cd}+foo*", 0) |> ignore

        r.Replace("{ab}*{ab}+{cd}+foo*", MatchEvaluator (fun m -> "{ab}*{ab}+{cd}+foo*") ) |> ignore
        r.Replace("{ab}*{ab}+{cd}+foo*", "{ab}*{ab}+{cd}+foo*") |> ignore
        r.Replace("{ab}*{ab}+{cd}+foo*", MatchEvaluator (fun m -> "{ab}*{ab}+{cd}+foo*"), 0) |> ignore
        r.Replace("{ab}*{ab}+{cd}+foo*", "{ab}*{ab}+{cd}+foo*", 0) |> ignore
        r.Replace("{ab}*{ab}+{cd}+foo*", MatchEvaluator (fun m -> "{ab}*{ab}+{cd}+foo*"), 0, 0) |> ignore
        r.Replace("{ab}*{ab}+{cd}+foo*", "{ab}*{ab}+{cd}+foo*", 0, 0) |> ignore

        r.Split("{ab}*{ab}+{cd}+foo*") |> ignore
        r.Split("{ab}*{ab}+{cd}+foo*", 0) |> ignore
        r.Split("{ab}*{ab}+{cd}+foo*", 0, 0) |> ignore

        // not hardcoded strings are compliant
        let r = new Regex(s)
        let r = new Regex(s, RegexOptions.Compiled, TimeSpan.Zero)
        Regex.Replace("{ab}*{ab}+{cd}+foo*", s, "{ab}*{ab}+{cd}+foo*", RegexOptions.Compiled, TimeSpan.Zero) |> ignore
        Regex.Split("{ab}*{ab}+{cd}+foo*", s, RegexOptions.Compiled, TimeSpan.Zero)
