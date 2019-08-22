module SonarAnalyzer.FSharp.RuleRunner

open FSharpAst

(* ===============================================
Run one or more rules
=============================================== *)

/// run a specific list of rules on a file
let analyzeFileWithRules (rules:Rule list) (filename:string) :Diagnostic list =
    let config = TransformerConfig.Default

    let results = ResizeArray()
    let accept ctx =
        for rule in rules do
            rule ctx |> Option.iter (fun result -> results.Add result)
        true // keep going
    let visitor = TastVisitor(accept)

    let tastResult = FileApi.translateFile config filename
    match tastResult with
    | Error errs ->
        errs |> Array.map Diagnostic.CompilationError |> Array.toList
    | Ok tast ->
        visitor.Visit(tast)
        results |> Seq.toList

/// run all the rules on a file
let analyzeFileWithAllRules (filename:string) :Diagnostic list =

    let availableRules = RuleManager.getAvailableRules()
    let rulesToUse =
        availableRules
        |> List.map (fun rule -> rule.Rule)

    filename |> analyzeFileWithRules rulesToUse

/// run all the rules on all the .FS files in a directory
let analyzeFilesWithAllRules (filenames:string seq) :Diagnostic list =

    let availableRules = RuleManager.getAvailableRules()
    let rulesToUse =
        availableRules
        |> List.map (fun rule -> rule.Rule)

    filenames
    |> Seq.toList
    |> List.collect (analyzeFileWithRules rulesToUse)

/// Use the config to determine which files to analyze
/// and which rules to use
let analyzeConfig (config:AnalysisConfig.Root) =

    let availableRules = RuleManager.getAvailableRules()
    let configuredRules =
        config.Rules |> List.map (fun rule -> rule.Key) |> Set.ofList
    let isConfigured ruleId =
        if configuredRules.IsEmpty then
            // if no rules configured, use all rules
            true
        else
            configuredRules |> Set.contains ruleId

    let rulesToUse =
        availableRules
        |> List.filter (fun rule -> isConfigured rule.RuleId)
        |> List.map (fun rule -> rule.Rule)

    let configuredFiles =
        config.Files |> List.map (fun file -> file.Filename)

    configuredFiles
    |> List.collect (analyzeFileWithRules rulesToUse)


