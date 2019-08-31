namespace SonarAnalyzer.FSharp

(*
===========================================
Types in the domain, such as rules, atributes, etc
===========================================
*)

open System
open System.Globalization
open System.Resources
open FSharpAst


// ===========================================
// Rule descriptions that are loaded from resource files
// ===========================================

type RuleSeverity =
    | Info
    | Minor
    | Major  // default
    | Critical
    | Blocker
    | Unknown
    with
    static member Default = RuleSeverity.Major

type RuleType =
    | CodeSmell // default
    | Bug
    | Vulnerability
    | SecurityHotspot
    | Unknown
    with
    static member Default = RuleType.CodeSmell

type RuleParameter = {
    Key : string
    Description : string
    Type : string
    DefaultValue : string
    }

type RuleDetail = {
    Key : string
    Title : string
    Description : string
    Severity : RuleSeverity
    Type : RuleType
    Tags : string list
    Parameters : RuleParameter list
    IsActivatedByDefault : bool
    CodeFixTitles : string list
    Remediation : string
    RemediationCost : string
    }
    with
    static member Default() = {
        Key = ""
        Title = ""
        Description = ""
        Severity = RuleSeverity.Default
        Type = RuleType.Default
        Tags = []
        Parameters = []
        IsActivatedByDefault = false
        CodeFixTitles = []
        Remediation = ""
        RemediationCost = ""
        }

// ===========================================
// Configuration for analysis (provided as an XML file on the command line)
// ===========================================

module rec AnalysisConfig =

    /// Determines which rules should be run, what parameters should be used,
    /// and which files to process.
    type Root = {
        Settings : Setting list
        RuleSelection: RuleSelection
        FileSelection: FileSelection
    }

    type Setting = {
        Key : string
        Value : string
    }

    type RuleSelection =
        | AllRules
        | SelectedRules of Rule list

    type Rule = {
        Key : string
        Parameters: RuleParameter list
    }

    type RuleParameter = {
        Key : string
        Value: string
    }

    type File = {
        Filename : string
    }

    type Project = {
        Filename : string
    }

    type FileSelection =
        | Projects of Project list
        | SelectedFiles of File list

// ===========================================
// Attribute types that are attached to rules
// ===========================================

/// The RuleAttribute MUST be attached to a rule method
/// in order for it to be detected using reflection.
/// It contains the key/ruleId.
[<AttributeUsage(AttributeTargets.Method, AllowMultiple = false)>]
type RuleAttribute(key:string) =
    inherit Attribute()
    member this.Key = key

type PropertyType =
    | String
    | Text
    | Password
    | Boolean
    | Integer
    | Float
    | SingleSelectList
    | Metric
    | License
    | RegularExpression
    | PropertySet
    | UserLogin


/// One or more RuleParameterAttributes MAY be attached to a rule method
/// in order to specify what parameters are needed.
[<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
type RuleParameterAttribute(key:string, ty:PropertyType, description:string, defaultValue:string) =
    inherit Attribute()

    new (key, ty, description, defaultValue:int) =
        let defaultValue : string = defaultValue.ToString(CultureInfo.InvariantCulture)
        RuleParameterAttribute(key, ty, description, defaultValue)

    new (key, ty, description) =
        RuleParameterAttribute(key, ty, description, null)

    new (key, ty) =
        RuleParameterAttribute(key, ty, null, null)

    member this.Key = key
    member this.PropertyType = ty
    member this.Description = description
    member this.DefaultValue = defaultValue

// ===========================================
// Diagnostic types
// ===========================================

/// Context-free information about a diagnostic, independent of a specific failure.
type DiagnosticDescriptor = {
    Id: string
    Title: string
    Description : string
    HelpLinkUri : string
    MessageFormat : string
    Category: string
    DefaultSeverity: RuleSeverity
    IsEnabledByDefault : bool
    CustomTags : string list
    }
    with
    //TODO change to avoid IO in the middle of the app.
    //Background: this was copied from the Sonar dotNet scanner. It loads data from the resource manager as part of setting
    //up a rule -- I don't think this necessary and could be refactored to be cleaner.

    /// Create a DiagnosticDescriptor
    static member Create(diagnosticId, messageFormat, resourceManager:ResourceManager, ?isEnabledByDefault, ?defaultSeverity) =
        let resourceKey key = sprintf "%s_%s" diagnosticId key
        let isEnabledByDefaultRes =
            resourceManager.GetString(resourceKey "IsActivatedByDefault" )
            |> bool.Parse
        let isEnabledByDefault = isEnabledByDefault |> Option.defaultValue isEnabledByDefaultRes
        let defaultSeverity = defaultSeverity |> Option.defaultValue RuleSeverity.Default
        let helpLink = String.Format(resourceManager.GetString("HelpLinkFormat"), diagnosticId.[1..])

        let customTags =
            let tags = ResizeArray<string>()
            tags.Add "FSharp"
            //if isEnabledByDefaultRes then tags.Add("SonarWay")
            tags |> Seq.toList

        {
            Id = diagnosticId
            Title = resourceManager.GetString(resourceKey "Title")
            MessageFormat = messageFormat
            Category = resourceManager.GetString(resourceKey "Category")
            DefaultSeverity = defaultSeverity
            IsEnabledByDefault = isEnabledByDefault
            HelpLinkUri = helpLink
            Description = resourceManager.GetString(resourceKey "Description")
            CustomTags = customTags
        }


/// Represents a diagnostic, such as a error or a warning, at a particular location where it occurred.
type Diagnostic = {
    Descriptor: DiagnosticDescriptor
    Location : Tast.Location
    Severity : RuleSeverity
    MessageArgs: obj[]
    }
    with

    /// Get a message formatted with the params
    member this.Message =
        String.Format(this.Descriptor.MessageFormat,this.MessageArgs)

    /// Create a new diagnostic
    static member Create(descriptor,location,[<ParamArray>] messageArgs) =
        {
        Descriptor = descriptor
        Location = location
        Severity = descriptor.DefaultSeverity
        MessageArgs = messageArgs
        }

    static member CompilationError(fsharpErrorInfo:FSharp.Compiler.SourceCodeServices.FSharpErrorInfo) =
        let severity = RuleSeverity.Blocker
        let location : Tast.Location = {
            FileName = fsharpErrorInfo.FileName
            StartLine = fsharpErrorInfo.StartLineAlternate
            StartColumn = fsharpErrorInfo.StartColumn
            EndLine = fsharpErrorInfo.EndLineAlternate
            EndColumn = fsharpErrorInfo.EndColumn
            }
        let descriptor : DiagnosticDescriptor = {
            Id = string fsharpErrorInfo.ErrorNumber
            Title = "Compilation Error"
            MessageFormat = "{0}"
            Category = ""
            DefaultSeverity = severity
            IsEnabledByDefault = false
            HelpLinkUri = ""
            Description = fsharpErrorInfo.Message
            CustomTags = []
            }
        {
        Descriptor = descriptor
        Location = location
        Severity = severity
        MessageArgs = [||]
        }

// ===========================================
// Definition of a Rule
// ===========================================

/// A rule takes a TAST context and, if triggered, returns an optional Diagnostic
type Rule = TastContext -> Diagnostic option

/// A rule detected in the assembly. These are loaded using reflection
/// to find methods with the "RuleAttribute"
type AvailableRule = {
    RuleId: string
    /// An executable rule
    Rule : Rule
    /// The attribute (required) associated with the rule
    RuleAttribute : RuleAttribute
    /// Any parameters associated with the rule (not used for security and vuln rules)
    RuleParameterAttributes : RuleParameterAttribute list
    /// The assembly the rule is in. This is used to find resources associated with the rule.
    Assembly : System.Reflection.Assembly
    }

