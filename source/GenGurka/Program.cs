using SpecGurka.GenGurka;
using TrxFileParser.Models;
using SpecGurka.GurkaSpec;
using System.Configuration;

Console.WriteLine("Hello, Gurka!");

var testProject = new TestProject();
var gurka = new Testrun { Name = "DemoProject" };
var gurkaProduct = new Product { Name = "Test Product" };
gurka.Products.Add(gurkaProduct);


// read gherkin
var featureFiles = GherkinFileReader.ReadFiles(testProject.FeaturesDirectory);
foreach (var featureFile in featureFiles)
{
    var gurkaFeature = new Feature { Name = featureFile.Feature.Name };
    gurkaFeature.Background = featureFile.Feature.Description;
    foreach (var featureChild in featureFile.Feature.Children)
    {
        if (featureChild is Gherkin.Ast.Scenario scenario)
        {
            var gurkaScenario = new Scenario { Name = scenario.Name };
            foreach (var step in scenario.Steps)
            {
                gurkaScenario.Steps.Add(new Step
                {
                    Text = $"{step.Keyword}{step.Text}",
                    Kind = step.Keyword,
                });
            }
            gurkaFeature.Scenarios.Add(gurkaScenario);
        }
        else if (featureChild is Gherkin.Ast.Rule rule)
        {
            var gurkaRule = new Rule { Name = rule.Name };
            foreach (var ruleChild in rule.Children)
            {
                if (ruleChild is Gherkin.Ast.Scenario rScenario)
                {
                     var gurkaScenario = new Scenario { Name = rScenario.Name };
                    foreach (var step in rScenario.Steps)
                    {
                        gurkaScenario.Steps.Add(new Step { Text = $"{step.Keyword}{step.Text}" });
                    }
                    gurkaRule.Scenarios.Add(gurkaScenario);
                }
            }
            gurkaFeature.Rules.Add(gurkaRule);
        }
    }
    gurkaProduct.Features.Add(gurkaFeature);
}
// read test dll
//var assembly = Assembly.LoadFile(testProject.AssemblyFile);
//within dll find all attributes of type Given
//var givenMethods = assembly.GetTypes()
//                     .SelectMany(t => t.GetMethods())
//                      .Where(m => m.GetCustomAttributes(typeof(GivenAttribute), false).Length > 0)
//                      .ToArray();

// read test result
TestRun testRun = TrxFileParser.TrxDeserializer.Deserialize(testProject.TestResultFile);

var sortedTestResults = testRun.Results.UnitTestResults
    .OrderBy(utr => gurkaProduct.Features.FindIndex(f => f.GetScenario(utr.TestName) != null))
    .ToList();


foreach (var feature in gurkaProduct.Features)
{
    bool featurePassed = true;
    foreach (var utr in sortedTestResults)
    {
        var sceUnderTest = feature.GetScenario(utr.TestName);
        if (sceUnderTest == null)
            continue;
        bool outcome = utr.Outcome == "Passed";
        sceUnderTest.TestPassed = outcome;
        sceUnderTest.TestOutput = utr.Output.StdOut;
        sceUnderTest.TestDurationTime = TimeSpan.Parse(utr.Duration);
        sceUnderTest.ParseTestOutput(utr.Output.StdOut);
        if (utr.Output.ErrorInfo != null)
            sceUnderTest.ParseTestError(utr.Output.ErrorInfo.Message);
        sceUnderTest.ErrorMessage = utr.Output.ErrorInfo.Message;
        if (!outcome)
            featurePassed = false;
    }
    feature.TestsPassed = featurePassed;
}

// var featUnderTest = gurkaProduct.GetFeature("Manage Company");
// bool featurePassed = true;
// foreach (var utr in testRun.Results.UnitTestResults)
// {
//
//     var sceUnderTest = featUnderTest.GetScenario(utr.TestName);
//     if(sceUnderTest == null)
//         continue;
//     bool outcome = utr.Outcome == "Passed" ? true : false;
//     sceUnderTest.TestPassed = outcome;
//     sceUnderTest.TestOutput = utr.Output.StdOut;
//     sceUnderTest.TestDurationTime = TimeSpan.Parse(utr.Duration);
//     sceUnderTest.ParseTestOutput(utr.Output.StdOut);
//     if (utr.Output.ErrorInfo != null)
//         sceUnderTest.ParseTestError(utr.Output.ErrorInfo.Message);
//         //sceUnderTest.ErrorMessage = utr.Output.ErrorInfo.Message;
//     if(outcome == false)
//         featurePassed = false;
//
//
// }
// featUnderTest.TestsPassed = featurePassed;

// testrun name
// testrun time
// all festures
// result
// exc time / duration
// secenarios within
// result
// exception
// exc time


Gurka.WriteGurkaFile(ConfigurationManager.AppSettings["outputpath"], gurka);

