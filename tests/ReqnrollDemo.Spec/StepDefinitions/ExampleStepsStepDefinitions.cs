namespace ReqnrollDemo.Spec.StepDefinitions;

using Reqnroll;
using System;
using System.Collections.Generic;

[Binding]
public class ExampleStepsStepDefinitions
{
    // Will be unique per scenario
    private readonly ScenarioContext _scenarioContext;

    public ExampleStepsStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"feature background step")]
    public void GivenFeatureBackgroundStep()
    {
        AddStep("feature background step");
    }

    [Given(@"rule background step")]
    public void GivenRuleBackgroundStep()
    {
        AddStep("rule background step");
    }

    [Given(@"scenario step")]
    public void GivenScenarioStep()
    {
        AddStep("scenario step");
    }

    private void AddStep(string step)
    {
        if (!_scenarioContext.ContainsKey("stepLog"))
            _scenarioContext["stepLog"] = new List<string>();
        ((List<string>)_scenarioContext["stepLog"]).Add(step);
    }

    [AfterScenario]
    public void AfterScenario()
    {
        if (_scenarioContext.TryGetValue("stepLog", out var logObj) && logObj is List<string> log)
        {
            Console.WriteLine("=== Step Execution Order ===");
            foreach (var entry in log)
                Console.WriteLine(entry);
            Console.WriteLine("===========================");
        }
    }
}