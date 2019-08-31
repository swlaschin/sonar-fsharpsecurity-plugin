module RuleDefinitionDto

open System.Xml
open System.Xml.Serialization
open SonarAnalyzer.FSharp

(*
A RuleDefinitions file contains all the rule details in XML format.
The format is defined at http://javadocs.sonarsource.org/7.9.1/apidocs/org/sonar/api/server/rule/RulesDefinitionXmlLoader.html

This module contains the definitions of exportable, XML-friendly types.
*)

let logger = Serilog.Log.Logger

type RuleSeverityDto =
    // use enums so that XmlSerializer is happy
    | INFO = 0
    | MINOR = 1
    | MAJOR = 2 // default
    | CRITICAL = 3
    | BLOCKER = 4


/// see http://javadocs.sonarsource.org/7.9.1/apidocs/org/sonar/api/rules/RuleType.html
type RuleTypeDto =
    // use enums so that XmlSerializer is happy
    | CODE_SMELL = 0 // default
    | BUG = 1
    | VULNERABILITY = 2
#if SECURITY_HOTSPOT_SUPPORTED
    //  doesn't seem to be suported by RulesDefinitionXmlLoader uet
    // "No enum constant org.sonar.api.rules.RuleType.SECURITY_HOTSPOT"
    | SECURITY_HOTSPOT = 3
#endif

[<CLIMutable>]
type RuleParameterDto = {
    [<XmlElement("key")>]
    Key : string

    [<XmlElement("description")>]
    Description : XmlCDataSection

    [<XmlElement("type")>]
    Type : string

    [<XmlElement("defaultValue ")>]
    DefaultValue : string
    }

[<CLIMutable>]
type RuleDetailDto = {
    /// Required key. Max length is 200 characters.
    [<XmlElement("key")>]
    Key : string

    /// Required name. Max length is 200 characters.
    [<XmlElement("name")>]
    Name: string

    /// Required description. No max length. -->
    [<XmlElement("description")>]
    Description : XmlCDataSection

    /// Possible values are INFO, MINOR, MAJOR (default), CRITICAL, BLOCKER.
    [<XmlElement("severity")>]
    Severity : RuleSeverityDto

    /// Type as defined by the SonarQube Quality Model. Possible values are CODE_SMELL (default), BUG and VULNERABILITY.
    [<XmlElement("type")>]
    Type : RuleTypeDto

    /// Optional tags. See org.sonar.api.server.rule.RuleTagFormat. The maximal length of all tags is 4000 characters.
    [<XmlElement("tag")>]
    Tags : string[]   // use Array rather than list for serialization

    /// Optional parameters
    [<XmlElement("param")>]
    Parameters : RuleParameterDto[]  // use Array rather than list for serialization

    /// Quality Model - type of debt remediation function
    /// See http://javadocs.sonarsource.org/7.9.1/apidocs/org/sonar/api/server/debt/DebtRemediationFunction.Type.html
    [<XmlElement("remediationFunction")>]
    Remediation : string

    /// Quality Model - base effort of debt remediation function. Must be defined only for some function types.
    /// See http://javadocs.sonarsource.org/7.9.1/apidocs/org/sonar/api/server/rule/RulesDefinition.DebtRemediationFunctions.html
    [<XmlElement("remediationFunctionBaseEffort")>]
    RemediationCost : string
    }

/// The root of the definitions file
[<CLIMutable>]
[<XmlRoot("rules", Namespace = "")>]
type RuleDefinitionsDto = {

    [<XmlElement("rule")>]
    Rules : RuleDetailDto[]
    }

module RuleParameterDto =

    let fromDomain (domain:RuleParameter) :RuleParameterDto =
        {
            Key = domain.Key
            Description = XmlDocument().CreateCDataSection(domain.Description)
            Type = domain.Type
            DefaultValue = domain.DefaultValue
        }

module RuleDetailDto =

    let toSeverity (severity:RuleSeverity) : RuleSeverityDto option =
        match severity with
        | Info -> Some RuleSeverityDto.INFO
        | Minor -> Some RuleSeverityDto.MINOR
        | Major -> Some RuleSeverityDto.MAJOR
        | Critical -> Some RuleSeverityDto.CRITICAL
        | Blocker -> Some RuleSeverityDto.BLOCKER
        | RuleSeverity.Unknown -> None

    let toType (ty:RuleType) : RuleTypeDto option =
        match ty with
        | Bug -> Some RuleTypeDto.BUG
        | CodeSmell -> Some RuleTypeDto.CODE_SMELL
        | Vulnerability  -> Some RuleTypeDto.VULNERABILITY
#if SECURITY_HOTSPOT_SUPPORTED
        | SecurityHotspot -> Some RuleTypeDto.SECURITY_HOTSPOT
#else
        | SecurityHotspot -> Some RuleTypeDto.VULNERABILITY
#endif
        | RuleType.Unknown -> None

    let fromDomain (domain:RuleDetail) :RuleDetailDto option =
        match toType domain.Type, toSeverity domain.Severity with
        | None, None ->
            logger.Warning("For rule {rule}, Type and Severity are unknown",domain.Key)
            None
        | Some _, None ->
            logger.Warning("For rule {rule}, Severity is unknown",domain.Key)
            None
        | None, Some _ ->
            logger.Warning("For rule {rule}, Type is unknown",domain.Key)
            None
        | Some ty, Some severity ->
            {
                Key = domain.Key
                Name = domain.Title
                Description = XmlDocument().CreateCDataSection(domain.Description)
                Severity = severity
                Type = ty
                Tags = domain.Tags |> List.toArray
                Parameters =
                    domain.Parameters
                    |> List.map RuleParameterDto.fromDomain
                    |> List.toArray
                Remediation = domain.Remediation
                RemediationCost = domain.RemediationCost
            } |> Some

module RuleDefinitionsDto =

    let toDto (availableRules:AvailableRule list) : RuleDefinitionsDto =

        let ruleDetailDtos =
            availableRules
            |> List.map RuleManager.toRuleDetail
            |> List.choose RuleDetailDto.fromDomain
            |> List.toArray // use array for serialization

        {
            Rules = ruleDetailDtos
        }

