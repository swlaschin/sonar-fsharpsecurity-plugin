module SonarAnalyzer.FSharp.RuleManager

(*
A collection of utitities for finding and managing the rules.
*)

open System
open System.Reflection
open System.Globalization

let logger = Serilog.Log.Logger
let loggerPrefix = "SonarAnalyzer.FSharp.RuleManager"


/// Return all the assemblies that have rules in them
let ruleContainingAssemblies() : Assembly list =
    [
    typeof<RuleAttribute>.Assembly
    ]

/// Get all rules and their associated attributes
/// from the ruleContainingAssemblies
let getAvailableRules() : AvailableRule list=

    let ruleMethods =
        ruleContainingAssemblies()
        |> Seq.collect (fun assembly -> assembly.GetTypes())
        |> Seq.collect (fun ty -> ty.GetMethods() )
        |> Seq.filter (fun m -> m.GetCustomAttributes<RuleAttribute>() |> Seq.isEmpty |> not )
        |> Seq.toList

    ruleMethods
    |> List.map (fun m ->
        let r: Rule =
            fun ctx -> m.Invoke(null,[|ctx|]) :?> Diagnostic option
        let a = m.GetCustomAttributes<RuleAttribute>() |> Seq.head
        let ps = m.GetCustomAttributes<RuleParameterAttribute>() |> Seq.toList
        {
            RuleId = a.Key
            Rule = r
            RuleAttribute = a
            RuleParameterAttributes = ps
            Assembly = m.DeclaringType.Assembly
        }
        )

let private getResourceHtml(rule:AvailableRule) =
    let ruleId = rule.RuleId

    let resources =
        ruleContainingAssemblies()
        |> Seq.collect (fun a -> a.GetManifestResourceNames() )

    let ruleDescriptionPathPattern = "SonarAnalyzer.FSharp.Rules.Description.{0}.html"

    let html =
        resources
        |> Seq.tryFind (fun r -> r = String.Format(ruleDescriptionPathPattern, ruleId) )
        |> Option.map (fun resourceId ->
            use stream = rule.Assembly.GetManifestResourceStream(resourceId)
            use reader = new IO.StreamReader(stream)
            reader.ReadToEnd()
            )
        |> Option.defaultWith (fun () ->
            logger.Error("[{prefix}] Could not locate HTML resource for rule '{ruleId}'", loggerPrefix,ruleId)
            "" // empty HTML
            )
    html

let private propertyTypeToString(propertyType:PropertyType) =
    let parts = System.Text.RegularExpressions.Regex.Split(propertyType.ToString(), @"(?<!^)(?=[A-Z])")
    String.Join("_", parts).ToUpper(CultureInfo.InvariantCulture);

/// Construct a RuleDetail from an AvailableRule.
/// This involves querying the resources for more detail.
let toRuleDetail(rule:AvailableRule) =
    let ruleId = rule.RuleId

    let resources = RspecStrings.ResourceManager
    let getString suffix =
        let resourceName = sprintf "%s_%s" ruleId suffix
        try
            resources.GetString(resourceName)
        with
        | ex ->
            logger.Error("[{prefix}] Could not get resource for name '{resourceName}' Exception: '{ex}'", loggerPrefix,resourceName,ex.Message)
            ""  // empty string

    let toType typeStr =
        match typeStr with
        | "BUG" | "Bug" -> RuleType.Bug
        | "CODE_SMELL" | "CodeSmell"  -> RuleType.CodeSmell
        | "VULNERABILITY" | "Vulnerability" -> RuleType.Vulnerability
        | "SECURITY_HOTSPOT" | "SecurityHostspot" -> RuleType.SecurityHotspot
        | _ -> RuleType.Unknown

    let toSeverity severityStr =
        match severityStr with
        | "INFO" | "Info" -> RuleSeverity.Info
        | "MINOR" | "Minor" -> RuleSeverity.Minor
        | "MAJOR" | "Major" -> RuleSeverity.Major
        | "CRITICAL" | "Critical" -> RuleSeverity.Critical
        | "BLOCKER" | "Blocker" -> RuleSeverity.Blocker
        | _ -> RuleSeverity.Unknown

    /// not sure what this is for?
    /// https://github.com/SonarSource/sonar-dotnet/blob/ff7413922f6565eccccda028d0d89f22d2a11c68/sonaranalyzer-dotnet/src/SonarAnalyzer.Utilities/RuleDetailBuilder.cs#L100
    let toSonarQubeRemediationFunction remediation =
        match remediation with
        | null -> null
        | "Constant/Issue" -> "CONSTANT_ISSUE"
        | _ -> null

    let tags = (getString "Tags").Split([|','|],StringSplitOptions.RemoveEmptyEntries) |> List.ofArray

    let parameters =
        rule.RuleParameterAttributes |> List.map (fun ruleParameter ->
            {
                DefaultValue = ruleParameter.DefaultValue
                Description = ruleParameter.Description
                Key = ruleParameter.Key
                Type = ruleParameter.PropertyType |> propertyTypeToString
            })

    let codeFixTitles = []

    let ruleDetail : RuleDetail =
        {
            Key = ruleId
            Title = getString "Title"
            Description = getResourceHtml(rule)
            Severity = getString "Severity" |> toSeverity
            Type = getString "Type" |> toType
            Tags = tags
            Parameters = parameters
            IsActivatedByDefault = bool.Parse(getString "IsActivatedByDefault")
            Remediation = getString "Remediation" |> toSonarQubeRemediationFunction
            RemediationCost = getString "RemediationCost"
            CodeFixTitles = codeFixTitles
        }

    ruleDetail
