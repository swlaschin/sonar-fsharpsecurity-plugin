module rec DiagnosticsDto

(*
The issues are written to XML files using the schema below.

IMPORTANT: Any changes to the schema must be coordinated with the java side,
where it is used to import the issues (see AnalysisResultImporter.java)

*)

open System.Xml.Serialization
open SonarAnalyzer.FSharp
open System

(*

It should look like the example below. For now there are only issues, but in the future this could be expanded to metrics, etc.

<AnalysisOutput>
  <Issues>
    <Issue>
        <RuleKey>S1313</Key>
        <Message>Make sure using this hardcoded IP address '192.168.0.1' is safe here.</Message>
        <AbsoluteFilePath>P:/git_repos/sonar-fsharp-security/sonaranalyzer-fsharp/tests/SonarAnalyzer.FSharp.UnitTest/TestCases/S1313_HardcodedIpAddress.fs</AbsoluteFilePath>
        <StartLine>18</StartLine>
        <StartColumn>14</StartColumn>
        <EndLine>18</EndLine>
        <EndColumn>27</EndColumn>
    </Issue>
    <Issue>
        <RuleKey>S1313</Key>
        <Message>Make sure using this hardcoded IP address '192.168.0.1' is safe here.</Message>
        <AbsoluteFilePath>P:/git_repos/sonar-fsharp-security/sonaranalyzer-fsharp/tests/SonarAnalyzer.FSharp.UnitTest/TestCases/S1313_HardcodedIpAddress.fs</AbsoluteFilePath>
        <StartLine>26</StartLine>
        <StartColumn>14</StartColumn>  <-- note: two different issues for the same file at different locations -->
        <EndLine>26</EndLine>
        <EndColumn>54</EndColumn>
    </Issue>
    <Issue>
        <RuleKey>S2077</Key>
        <Message>Make sure that executing SQL queries is safe here.</Message>
        <AbsoluteFilePath>P:/git_repos/sonar-fsharp-security/sonaranalyzer-fsharp/tests/SonarAnalyzer.FSharp.UnitTest/TestCases/S2077_ExecutingSqlQueries.fs</AbsoluteFilePath>
        <StartLine>26</StartLine>
        <StartColumn>14</StartColumn>
        <EndLine>26</EndLine>
        <EndColumn>54</EndColumn>
    </Issue>
  </Issues>
</AnalysisOutput>

*)



[<CLIMutable>]
[<XmlRoot("AnalysisOutput", Namespace = "")>]
type RootDto = {

    [<XmlArray("Issues")>]
    [<XmlArrayItem("Issue")>]
    Issues: IssueDto[]

    }

[<CLIMutable>]
[<XmlRoot("Issue", Namespace = "")>]
type IssueDto  = {

    [<XmlElement("RuleKey")>]
    RuleKey : string

    [<XmlElement("Message")>]
    Message : string

    [<XmlElement("AbsoluteFilePath")>]
    AbsoluteFilePath : string

    [<XmlElement("StartLine")>]
    StartLine : int

    [<XmlElement("StartColumn")>]
    StartColumn : int

    [<XmlElement("EndLine")>]
    EndLine : int

    [<XmlElement("EndColumn")>]
    EndColumn : int

    }

let logger = Serilog.Log.Logger

let absoluteFilePath filename =
    let f = System.IO.FileInfo(filename)
    f.FullName


module IssueDto =

    let fromDiagnostic (diagnostic:Diagnostic) : IssueDto =

        let ruleKey = diagnostic.Descriptor.Id
        let message = diagnostic.Message
        let filename = diagnostic.Location.FileName
        let startLine = diagnostic.Location.StartLine
        let startColumn = diagnostic.Location.StartColumn
        let endLine = diagnostic.Location.EndLine
        let endColumn = diagnostic.Location.EndColumn

        // log it
        logger.Warning("{filename}({row},{col}): {RuleId}: {message}",
            filename,
            startLine,
            startColumn,
            ruleKey,
            message
            )

        {
            RuleKey = ruleKey
            Message = message
            AbsoluteFilePath = filename |> absoluteFilePath
            StartLine = startLine
            StartColumn = startColumn
            EndLine = endLine
            EndColumn = endColumn
        }



let toDto (diagnostics:Diagnostic list) : RootDto =

    let issues = diagnostics |> List.map IssueDto.fromDiagnostic
    {
        Issues = issues |> List.toArray
    }

