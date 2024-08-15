using SpecGurka.GenGurka;
using System.Reflection;
using Reqnroll;
using TrxFileParser.Models;
using SpecGurka.GurkaSpec;
using System.Xml.Serialization;
using System.Xml;
using System.Configuration;

Console.WriteLine("Hello, Gurka!");

var gurka = new Testrun();
gurka.TestName = "DemoProject";
var gurkaProduct = new Product() { Name = "Test Product" };
gurka.Products.Add(gurkaProduct);


// read gherkin
var gherkinReader = new GherkinFileReader();
var featureFile = gherkinReader.ReadGherkinFile(ConfigurationManager.AppSettings["gherkinfile"]); 
var gurkaFeature = new Feature() { Name = featureFile.Feature.Name };
foreach (var featurechild in featureFile.Feature.Children)
{
    if (featurechild is not Gherkin.Ast.Scenario)
        continue;

    var s = (Gherkin.Ast.Scenario)featurechild;
    var gurkaScenario = new Scenario() { Name = s.Name };

    foreach(var step in s.Steps)
    {
        gurkaScenario.Steps.Add(new Step() { Text = step.Text });
    }
    gurkaFeature.Scenarios.Add(gurkaScenario);
}

gurkaProduct.Features.Add(gurkaFeature);
// read test dll
var assembly = Assembly.LoadFile(ConfigurationManager.AppSettings["assemblyfile"]); 
//within dll find all attributes of type Given
var givenMethods = assembly.GetTypes()
                      .SelectMany(t => t.GetMethods())
                      .Where(m => m.GetCustomAttributes(typeof(GivenAttribute), false).Length > 0)
                      .ToArray();

// read test result
TestRun testRun = TrxFileParser.TrxDeserializer.Deserialize(ConfigurationManager.AppSettings["trxfile"]); 

var featUnderTest = gurkaProduct.GetFeature("DemoProject");
bool featurePassed = true;
foreach (var utr in testRun.Results.UnitTestResults)
{

    var sceUnderTest = featUnderTest.GetScenario(utr.TestName);
    bool outcome = utr.Outcome == "Passed" ? true : false;
    sceUnderTest.TestPassed = outcome;
    sceUnderTest.TestOutput = utr.Output.StdOut;
    sceUnderTest.TestDurationTime = TimeSpan.Parse(utr.Duration);
    sceUnderTest.ParseTestOutput(utr.Output.StdOut);
    if (utr.Output.ErrorInfo != null)
        sceUnderTest.ParseTestError(utr.Output.ErrorInfo.Message);
        //sceUnderTest.ErrorMessage = utr.Output.ErrorInfo.Message;
    if(outcome == false)
        featurePassed = false;

    
}
featUnderTest.TestsPassed = featurePassed;

// testrun name
// testrun time
// all festures
// result
// exc time / duration
// secenarios within
// result
// exception
// exc time



XmlSerializer xsSubmit = new XmlSerializer(typeof(Testrun));
var xml = "";

using (var sww = new StringWriter())
{
    using (XmlWriter writer = XmlWriter.Create(sww))
    {
        xsSubmit.Serialize(writer, gurka);
        xml = sww.ToString(); // Your XML
    }
}

System.IO.File.WriteAllText($"{ConfigurationManager.AppSettings["outputpath"]}testrun_{DateTime.UtcNow.ToString("s").Replace(':','_')}.gurka", xml); 

Console.WriteLine("heelo");

// combine
// generate .gurka