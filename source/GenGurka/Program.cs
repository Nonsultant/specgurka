using SpecGurka.GenGurka;
using TrxFileParser.Models;
using SpecGurka.GurkaSpec;
using System.Globalization;
using Gherkin.Ast;
using SpecGurka.GenGurka.Extensions;
using SpecGurka.GenGurka.Helpers;
using Feature = SpecGurka.GurkaSpec.Feature;
using System.Diagnostics;


if (args.Contains("--help"))
    HelpMessage.Show();

var arguments = Arguments.ToDictionary(args);

var testProject = new TestProject();

testProject.ApplyArgumentConfiguration(arguments);

Console.WriteLine("Starting generation of Gurka file...");

var gurka = new Testrun
{
    Name = testProject.ProjectName,
    RunDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture),
    BaseUrl = testProject.BaseUrl ?? string.Empty
};

var gurkaProject = new Product { Name = testProject.ProjectName };
gurka.Products.Add(gurkaProject);

gurka.BranchName = GitHelpers.GetBranchName(testProject.FeaturesDirectory!);
gurka.CommitId = GitHelpers.GetLatestCommitId(testProject.FeaturesDirectory!);
gurka.CommitAuthor = GitHelpers.GetLatestCommitAuthor(testProject.FeaturesDirectory!);
gurka.CommitDate = GitHelpers.GetLatestCommitDate(testProject.FeaturesDirectory!);
gurka.CommitMessage = GitHelpers.GetLatestCommitMessage(testProject.FeaturesDirectory!);


Dictionary<string, GherkinDocument> gherkinFiles = GherkinFileReader.ReadFiles(testProject.FeaturesDirectory!);
// read all gherkin files including directories
foreach (var file in gherkinFiles)
{
    var filePath = file.Key.Replace("\\", "/");
    var gherkinDoc = file.Value;
    Feature gurkaFeature = gherkinDoc.Feature.ToGurkaFeature();

    gurkaFeature.FilePath = filePath;
    gurkaProject.Features.Add(gurkaFeature);
}

// read test result from dotnet test command
TestRun testRun = TrxFileParser.TrxDeserializer.Deserialize(testProject.TestResultFile);

// match test result with gurka features and adds the result to the gurka project
testRun.MatchWithGurkaFeatures(gurkaProject);

if (!System.IO.Directory.Exists(testProject.OutputPath!))
{
    System.IO.Directory.CreateDirectory(testProject.OutputPath!);
    Console.WriteLine($"Directory created: {testProject.OutputPath}");
}

var outputfile = Gurka.WriteGurkaFile(testProject.OutputPath!, gurka);

Console.WriteLine($"Gurka file created: {outputfile}");