module SonarAnalyzer.FSharp.RuleRunner

open FSharpAst

let logger = Serilog.Log.Logger
let loggerPrefix = "SonarAnalyzer.FSharp.RuleRunner"

(* ===============================================
Run one or more rules
=============================================== *)

/// run a specific list of rules on a file
let analyzeFileWithRules (rules:Rule list) (filename:string) :Diagnostic list =
    logger.Information("[{prefix}] Analyzing {filename} ", loggerPrefix, filename)

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

    |> fun results -> logger.Information("[{prefix}] ... {count} issues found", loggerPrefix, results.Length); results

/// run all the rules on a file
let analyzeFileWithAllRules (filename:string) :Diagnostic list =

    let availableRules = RuleManager.getAvailableRules()
    let rulesToUse =
        availableRules
        |> List.map (fun rule -> rule.Rule)

    filename |> analyzeFileWithRules rulesToUse


/// analyze everything as specified in the AnalysisConfig
let analyzeConfig (root:AnalysisConfig.Root)  :Diagnostic list =

    let availableRules = RuleManager.getAvailableRules()
    let rulesToUse =
        match root.RuleSelection with
        | AnalysisConfig.AllRules ->
            availableRules
        | AnalysisConfig.SelectedRules selectedRules ->
            let isSelected (availableRule:AvailableRule) =
                selectedRules |> List.exists (fun selectedRule -> selectedRule.Key = availableRule.RuleId)
            availableRules |> List.filter isSelected

        |> List.map (fun availableRule -> availableRule.Rule)

    logger.Information("[{prefix}] Analyzing {ruleCount} rules", loggerPrefix, rulesToUse.Length)

    match root.FileSelection with
    | AnalysisConfig.SelectedFiles files ->
        files
        |> fun files -> logger.Information("[{prefix}] Analyzing {fileCount} files", loggerPrefix, files.Length); files
        |> List.collect (fun file -> analyzeFileWithRules rulesToUse file.Filename)

    | AnalysisConfig.Projects _ -> failwithf "Projects not yet implemented"




