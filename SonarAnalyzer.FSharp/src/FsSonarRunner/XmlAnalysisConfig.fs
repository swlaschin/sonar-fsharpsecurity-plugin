module XmlAnalysisConfig

// convert from Sonar rule XML format to the Config domain type

open FSharp.Data
open SonarAnalyzer.FSharp

type SonarXmlAnalysisConfig = XmlProvider<"""
<AnalysisInput>
  <Settings>
    <Setting>
      <Key>sonar.cs.ignoreHeaderComments</Key>
      <Value>xxx</Value>
    </Setting>
    <Setting>
      <Key>sonar.cs.ignoreHeaderComments</Key>
      <Value>xxx</Value>
    </Setting>
  </Settings>
  <Rules>
    <Rule>
      <Key>FileLoc</Key>
      <Parameters>
        <Parameter>
          <Key>maxThreshold</Key>
          <Value>xxx</Value>
        </Parameter>
        <Parameter>
          <Key>minThreshold</Key>
          <Value>xxx</Value>
        </Parameter>
      </Parameters>
    </Rule>
    <Rule>
      <Key>SwitchWithoutDefault</Key>
    </Rule>
  </Rules>
  <Files>
    <File>E:\file.fs</File>
    <File>E:\file2.fs</File>
  </Files>
</AnalysisInput>
""">


/// Convert the XML document to a domain value
let toDomainConfig xmlFilename =
    let xml = SonarXmlAnalysisConfig.Parse(xmlFilename)

    let toSetting (s:SonarXmlAnalysisConfig.Setting) : AnalysisConfig.Setting =
        {Key = s.Key; Value = s.Value}
    let toFile (s:string) : AnalysisConfig.File =
        {Filename = s}
    let toParameter (s:SonarXmlAnalysisConfig.Parameter)  : AnalysisConfig.RuleParameter =
        {Key = s.Key; Value = s.Value}
    let toRule (s:SonarXmlAnalysisConfig.Rule)  : AnalysisConfig.Rule =
        {
        Key = s.Key
        Parameters =
            match s.Parameters with
            | Some ps -> ps.Parameters |> (Array.map toParameter) |> Array.toList
            | None -> []
        }

    let settings : AnalysisConfig.Setting list =
        xml.Settings |> Array.map toSetting |> Array.toList
    let rules : AnalysisConfig.Rule list =
        xml.Rules |> Array.map toRule |> Array.toList
    let files : AnalysisConfig.File list =
        xml.Files |> Array.map toFile |> Array.toList

    let config : AnalysisConfig.Root =
        {
        Settings = settings
        Rules = rules
        Files = files
        }
    config