using System.Diagnostics;
using SpecGurka.GenGurka;
using TrxFileParser.Models;
using SpecGurka.GurkaSpec;
using System.Globalization;
using Gherkin.Ast;
using SpecGurka.GenGurka.Extensions;
using SpecGurka.GenGurka.Helpers;
using Feature = SpecGurka.GurkaSpec.Feature;

if (args.Contains("--help"))
    HelpMessage.Show();

var arguments = Arguments.ToDictionary(args);

var testProject = new TestProject();

using(var process = new Process())
{
    process.StartInfo.FileName = "dotnet";
    process.StartInfo.Arguments = "test --logger trx";
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.CreateNoWindow = true;
    process.Start();
    process.WaitForExit();
}

testProject.ApplyArgumentConfiguration(arguments);

Console.WriteLine("Starting generation of Gurka file...");

var gurka = new Testrun
{
    Name = testProject.ProjectName,
    DateAndTime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
};

var gurkaProject = new Product { Name = testProject.ProjectName };
gurka.Products.Add(gurkaProject);

testProject.FeaturesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Features");

List<GherkinDocument> gherkinFiles = GherkinFileReader.ReadFiles(testProject.FeaturesDirectory);

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

testProject.TestResultFile = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestResults"),
    "*.trx").FirstOrDefault();


// read test result from dotnet test command
TestRun testRun = TrxFileParser.TrxDeserializer.Deserialize(testProject.TestResultFile);

// match test result with gurka features and adds the result to the gurka project
testRun.MatchWithGurkaFeatures(gurkaProject);

testProject.OutputPath = $"{Directory.GetCurrentDirectory()}/";

Console.WriteLine(testProject.OutputPath);

Gurka.WriteGurkaFile(testProject.OutputPath, gurka);

Console.WriteLine("Gurka file generated");