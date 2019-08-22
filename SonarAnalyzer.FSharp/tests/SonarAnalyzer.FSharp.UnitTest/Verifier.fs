module SonarAnalyzer.FSharp.UnitTest.Verifier

(*
Run a rule on a file and verify that the errors detected by the rule
match the errors we expect.

To get the locations of the expected errors, parse the file
looking for lines tagged with a "// Noncompliant" comment.
*)


open System
open System.Text.RegularExpressions
open NUnit.Framework

let getNoncompliantLocations lines =

    // The pattern means match "<lineStart><anything>//<space>Noncompliant"
    // but "<anything>" can't contain "/".
    // This prevents commented-out lines from showing up.
    let regexPattern = @"^[^/]*//\s*Noncompliant"

    let getLocation lineNo lineText =
        let m = Regex.Match(lineText, regexPattern)
        if (m.Success) then
            Some (lineNo + 1)
        else
            None

    lines
    |> List.mapi getLocation
    |> List.choose id

    (*
    getNoncompliantLocations [
        @"1. // Compliant"
        @"2. normal code // Noncompliant"
        @"2. // commented out code // Noncompliant"
        @"3. Noncompliant() // a call not a comment"
        ]
    *)

let applyRule fileName rule =
    let config = FSharpAst.TransformerConfig.Default
    let tast = FSharpAst.FileApi.translateFile config fileName
    []

let verify fileName rule =
    try
        let lines = IO.File.ReadAllLines fileName |> List.ofArray
        let expectedLocations =
            getNoncompliantLocations lines
            |> sprintf "Line numbers %A" // convert to a string for easier testing
        let actualLocations =
            SonarAnalyzer.FSharp.RuleRunner.analyzeFileWithRules [rule] fileName
            |> List.map (fun diag -> diag.Location.StartLine)
            |> List.distinct |> List.sort
            |> sprintf "Line numbers %A" // convert to a string for easier testing
        Assert.AreEqual(expectedLocations, actualLocations)
    with
    | ex ->
        Assert.Fail(sprintf "[%s] %s" fileName ex.Message)
        reraise()
