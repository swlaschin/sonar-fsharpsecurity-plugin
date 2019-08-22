module RuleDescriptorFiles

(*
The RuleDescriptor files contain all the rule details in XML format
and are used by Sonar to display information.

This module contains logic to extract them and export as XML files.
*)

open SonarAnalyzer.FSharp
open RuleDescriptorDto
open System
open System.Xml
open System.IO
open System.Xml.Serialization
open System.Text


let ruleFile = "rules.xml"
let profileFile = "profile.xml"
let lang = "fsharp"

let logger = Serilog.Log.Logger


let serializeObjectToFile filePath (objectToSerialize:obj) =
    let settings = XmlWriterSettings(Indent = true,Encoding = Encoding.UTF8,IndentChars = "  ")

    use stream = new MemoryStream()
    use writer = XmlWriter.Create(stream, settings)
    let serializer = new XmlSerializer(objectToSerialize.GetType())
    let namespaces = XmlSerializerNamespaces [|XmlQualifiedName.Empty |]
    serializer.Serialize(writer, objectToSerialize, namespaces)
    let ruleXml = Encoding.UTF8.GetString(stream.ToArray())
    IO.File.WriteAllText(filePath, ruleXml)

let writeRuleDescriptorFile filePath (ruleDetailDtos:RuleDetailDto[]) =
    logger.Information (sprintf "Writing %s" filePath)
    let root : RuleDescriptorRoot = {
        Rules = ruleDetailDtos
    }
    serializeObjectToFile filePath root

let writeQualityProfileFile filePath (ruleDetailDtos:RuleDetailDto[]) =
    logger.Information (sprintf "Writing %s" filePath)
    let name = "Sonar way"
    let rules : QualityProfileRuleDescriptor [] =
        ruleDetailDtos
        |> Array.filter (fun rule -> rule.IsActivatedByDefault)
        |> Array.map (fun rule ->
            {
                RepositoryKey = lang
                Key = rule.Key
            })

    let root : QualityProfileRoot = {
        Name = name
        Language = lang
        Rules = rules
    }
    serializeObjectToFile filePath root

/// Write all available rules to the rules file and profile file
let write(dirname) =
    logger.Information "Starting to write XmlDescriptorFiles"

    let rules = RuleManager.getAvailableRules()

    let ruleDetailDtos =
        rules
        |> List.map RuleManager.toRuleDetail
        |> List.map RuleDetailDto.fromDomain
        |> List.toArray // use array for serialization

    let targetPath = IO.Path.Combine(dirname,lang)
    IO.Directory.CreateDirectory(targetPath ) |> ignore

    let rulePath = IO.Path.Combine(targetPath, ruleFile)
    writeRuleDescriptorFile rulePath ruleDetailDtos

    let profilePath = IO.Path.Combine(targetPath, profileFile)
    writeQualityProfileFile profilePath ruleDetailDtos

    logger.Information "Done"