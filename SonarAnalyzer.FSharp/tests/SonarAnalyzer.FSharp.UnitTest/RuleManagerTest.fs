module SonarAnalyzer.FSharp.UnitTest.RuleFinderTest

open SonarAnalyzer.FSharp
open NUnit.Framework
open FSharpAst

// check that the Rule manager logic work

/// Check that all rules loaded in the assembly actually run witout error
[<Test>]
let ``check all AvailableRules can run without error``() =

    let availableRules = RuleManager.getAvailableRules()
    if Seq.length availableRules = 0 then
        Assert.Fail("Expect non empty list of rules")

    /// create a dummy context to run each rule on
    let dummyNode : Tast.ImplementationFile = {Name= "dummy"; Decls=[]}
    let ctx : TastContext = {Filename=dummyNode.Name; Node=dummyNode; Ancestors=[]}

    let failedRules = ResizeArray()
    for availableRule in availableRules do
        let ruleId = availableRule.RuleId
        try
            let _result = availableRule.Rule ctx
            ()
        with
        | ex ->
            let msg = sprintf "Rule %s failed with exception '%s'\n%s\n===============" ruleId ex.Message ex.StackTrace
            failedRules.Add msg

    if failedRules.Count > 0 then
        let msg = String.concat "\n" failedRules
        Assert.Fail(msg)

/// Check that all rules loaded in the assembly can be converted into RuleDetails
/// and that their associated resources are available.
[<Test>]
let ``check all RuleDetails can be created``() =

    let availableRules = RuleManager.getAvailableRules()
    if Seq.length availableRules = 0 then
        Assert.Fail("Expect non empty list of rules")

    let failedRules = ResizeArray()
    for availableRule in availableRules do
        try
            let _ruleDetail = RuleManager.toRuleDetail availableRule
            ()
        with
        | ex ->
            let msg = sprintf "Rule %s failed with exception '%s'\n%s\n===============" availableRule.RuleId ex.Message ex.StackTrace
            failedRules.Add msg

    if failedRules.Count > 0 then
        let msg = String.concat "\n" failedRules
        Assert.Fail(msg)
