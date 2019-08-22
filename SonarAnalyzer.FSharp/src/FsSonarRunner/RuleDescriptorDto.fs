module RuleDescriptorDto

open System.Xml
open System.Xml.Serialization
open SonarAnalyzer.FSharp

(*
The RuleDescriptor files contain all the rule details in XML format
and are used by Sonar to display information.

This module contains the exportable, XML-friendly versions of the domain types.
*)

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
    [<XmlElement("key")>]
    Key : string

    [<XmlElement("type")>]
    Type : string

    [<XmlElement("title")>]
    Title : string

    [<XmlElement("severity")>]
    Severity : string

    [<XmlElement("description")>]
    Description : XmlCDataSection

    [<XmlElement("tag")>]
    Tags : string[]   // use Array rather than list for serialization

    [<XmlElement("param")>]
    Parameters : RuleParameterDto[]  // use Array rather than list for serialization

    [<XmlIgnore>]
    IsActivatedByDefault : bool

    [<XmlElement("remediationFunction")>]
    Remediation : string

    [<XmlElement("remediationFunctionBaseEffort")>]
    RemediationCost : string
    }

[<CLIMutable>]
[<XmlRoot("rules", Namespace = "")>]
type RuleDescriptorRoot = {

    [<XmlElement("rule")>]
    Rules : RuleDetailDto[]
    }

[<CLIMutable>]
[<XmlType("rule")>]
type QualityProfileRuleDescriptor = {

    [<XmlElement("repositoryKey")>]
    RepositoryKey : string

    [<XmlElement("key")>]
    Key : string
    }

[<CLIMutable>]
[<XmlRoot("profile", Namespace = "")>]
type QualityProfileRoot = {

    [<XmlElement("language")>]
    Language : string

    [<XmlElement("name")>]
    Name : string

    [<XmlArray("rules")>]
    Rules : QualityProfileRuleDescriptor[]
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

    let fromDomain (domain:RuleDetail) :RuleDetailDto =
        {
            Key = domain.Key
            Type = domain.Type
            Title = domain.Title
            Severity = domain.Severity
            Description = XmlDocument().CreateCDataSection(domain.Description)
            Tags = domain.Tags |> List.toArray
            Parameters =
                domain.Parameters
                |> List.map RuleParameterDto.fromDomain
                |> List.toArray
            IsActivatedByDefault = domain.IsActivatedByDefault
            Remediation = domain.Remediation
            RemediationCost = domain.RemediationCost
        }