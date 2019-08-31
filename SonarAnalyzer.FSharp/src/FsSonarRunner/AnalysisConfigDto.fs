module rec AnalysisConfigDto

(*
The input for the analysis is:
(a) a list of settings
(b) a list of rules and associated parameters
(c) a list of files to run the rules on

In the java-based plugin, these are passed in from the SonarScanner.
The plugin then writes this out as a XML file (this AnalysisConfig DTO)
The F# runner then reads the config file in, converts it to the domain type, and then passed that the analyzer

*)
// convert from Sonar rule XML format to the Config domain type

open System.Xml.Serialization
open SonarAnalyzer.FSharp

(*

It should look like this

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


*)

[<CLIMutable>]
[<XmlRoot("AnalysisInput", Namespace = "")>]
type RootDto = {

    [<XmlArray("Settings", IsNullable = true)>]
    [<XmlArrayItem("Setting")>]
    Settings : SettingDto[]

    [<XmlArray("Rules", IsNullable = true)>]
    [<XmlArrayItem("Rule")>]
    Rules : RuleDto[]

    [<XmlArray("Files", IsNullable = true)>]
    [<XmlArrayItem("File")>]
    Files: FileDto[]
    }

[<CLIMutable>]
[<XmlRoot("Setting", Namespace = "")>]
type SettingDto  = {

    [<XmlElement("Key")>]
    Key : string

    [<XmlElement("Value")>]
    Value : string
    }

[<CLIMutable>]
[<XmlRoot("Rule", Namespace = "")>]
type RuleDto  = {
    [<XmlElement("Key")>]
    Key : string

    [<XmlArray("Parameters", IsNullable = true)>]
    [<XmlArrayItem("Parameter")>]
    Parameters : ParameterDto[]
    }

[<CLIMutable>]
[<XmlRoot("Parameter", Namespace = "")>]
type ParameterDto  = {
    [<XmlElement("Key")>]
    Key : string

    [<XmlElement("Value")>]
    Value : string
    }

[<CLIMutable>]
[<XmlRoot("File", Namespace = "")>]
type FileDto  = string



/// Convert the DTO to a domain value
let toDomain (dto:RootDto) : AnalysisConfig.Root =

    let toSetting (dto:SettingDto) : AnalysisConfig.Setting =
        {Key = dto.Key; Value = dto.Value}
    let toParameter (dto:ParameterDto)  : AnalysisConfig.RuleParameter =
        {Key = dto.Key; Value = dto.Value}
    let toRule (dto:RuleDto)  : AnalysisConfig.Rule =
        {
        Key = dto.Key
        Parameters = dto.Parameters |> (Array.map toParameter) |> Array.toList
        }
    let toFile (dto:FileDto) : AnalysisConfig.File =
        {Filename = dto}

    let settings =
        dto.Settings |> Array.map toSetting |> Array.toList
    let rules =
        dto.Rules |> Array.map toRule |> Array.toList
    let files =
        dto.Files |> Array.map toFile |> Array.toList

    let config : AnalysisConfig.Root =
        {
        Settings = settings
        RuleSelection = if rules.IsEmpty then AnalysisConfig.RuleSelection.AllRules else AnalysisConfig.RuleSelection.SelectedRules rules
        FileSelection = AnalysisConfig.FileSelection.SelectedFiles files
        }
    config

/// Convert the domain object to a DTO
let fromDomain (domain:AnalysisConfig.Root) :RootDto =

    let toSetting (domain:AnalysisConfig.Setting) : SettingDto =
        {Key = domain.Key; Value = domain.Value}
    let toParameter (domain:AnalysisConfig.RuleParameter) :ParameterDto =
        {Key = domain.Key; Value = domain.Value}
    let toRule (domain:AnalysisConfig.Rule) :RuleDto =
        {
        Key = domain.Key
        Parameters = domain.Parameters |> (List.map toParameter) |> Array.ofList
        }
    let toRuleFromAvailableRule (domain:AvailableRule) :RuleDto =
        {
        Key = domain.RuleId
        Parameters = [||]
        }
    let toFile (domain:AnalysisConfig.File) : FileDto =
        domain.Filename

    let settings =
        domain.Settings |> List.map toSetting |> Array.ofList
    let rules =
        match domain.RuleSelection with
        | AnalysisConfig.RuleSelection.AllRules ->
            // when saving, get all the rules that were used
            let availableRules = RuleManager.getAvailableRules()
            availableRules |> List.map toRuleFromAvailableRule |> Array.ofList
        | AnalysisConfig.RuleSelection.SelectedRules rules ->
            rules |> List.map toRule |> Array.ofList
    let files =
        match domain.FileSelection with
        | AnalysisConfig.FileSelection.SelectedFiles files ->
            files |> List.map toFile |> Array.ofList
        | AnalysisConfig.FileSelection.Projects _ ->
            failwith "FileSelection.Projects not implemented"

    let config : RootDto =
        {
        Settings = settings
        Rules = rules
        Files = files
        }
    config

