using SpecGurka.GenGurka;
using TrxFileParser.Models;
using SpecGurka.GurkaSpec;
using System.Configuration;

Console.WriteLine("Starting generation of Gurka file...");

var gurka = new Testrun
{
    Name = "DemoProject",
    Date = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
};

var gurkaProject = new Product { Name = "Company Management" };
gurka.Products.Add(gurkaProject);

// read gherkin
var featureFiles = GherkinFileReader.ReadFiles(TestProject.FeaturesDirectory);
foreach (var featureFile in featureFiles)
{
    var gurkaFeature = new Feature { Name = featureFile.Feature.Name };
    gurkaFeature.Description = featureFile.Feature.Description;
    foreach (var featureChild in featureFile.Feature.Children)
    {
        if (featureChild is Gherkin.Ast.Scenario scenario)
        {
            var gurkaScenario = new Scenario { Name = scenario.Name };
            foreach (var step in scenario.Steps)
            {
                gurkaScenario.Steps.Add(new Step
                {
                    Text = step.Text,
                    Kind = step.Keyword.Trim()
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
                        gurkaScenario.Steps.Add(new Step
                        {
                            Text = step.Text,
                            Kind = step.Keyword.Trim()
                        });
                    }
                    gurkaRule.Scenarios.Add(gurkaScenario);
                }
            }
            gurkaFeature.Rules.Add(gurkaRule);
        }
    }
List<GherkinDocument> gherkinFiles = GherkinFileReader.ReadFiles(TestProject.FeaturesDirectory);

gherkinFiles.ForEach(file =>
{
    Feature gurkaFeature = file.ToGurkaFeature();
    gurkaProject.Features.Add(gurkaFeature);
});

// read test dll
//var assembly = Assembly.LoadFile(testProject.AssemblyFile);
//within dll find all attributes of type Given
//var givenMethods = assembly.GetTypes()
//                     .SelectMany(t => t.GetMethods())
//                      .Where(m => m.GetCustomAttributes(typeof(GivenAttribute), false).Length > 0)
//                      .ToArray();

// read test result from dotnet test command
TestRun testRun = TrxFileParser.TrxDeserializer.Deserialize(TestProject.TestResultFile);

// match test result with gurka features and adds the result to the gurka project
testRun.MatchWithGurkaFeatures(gurkaProject);

Gurka.WriteGurkaFile(TestProject.OutputPath, gurka);

Console.WriteLine("Gurka file generated");