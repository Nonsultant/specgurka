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

//DotNetTestRunner.Run();

testProject.ApplyArgumentConfiguration(arguments);

Console.WriteLine("Starting generation of Gurka file...");

var gurka = new Testrun
{
    Name = testProject.ProjectName,
    DateAndTime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
};

var gurkaProject = new Product { Name = testProject.ProjectName };
gurka.Products.Add(gurkaProject);

string latestCommitId = GetLatestCommitId(testProject.FeaturesDirectory!);
gurkaProject.CommitId = latestCommitId;

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

static string GetLatestCommitId(string repositoryPath)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = "log -1 --pretty=\"%H\"",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        WorkingDirectory = repositoryPath
    };

    using (var process = new Process { StartInfo = startInfo })
    {
        process.Start();
        string commitId = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return commitId;
    }
}


// read test dll
//var assembly = Assembly.LoadFile(testProject.AssemblyFile);
//within dll find all attributes of type Given
//var givenMethods = assembly.GetTypes()
//                     .SelectMany(t => t.GetMethods())
//                      .Where(m => m.GetCustomAttributes(typeof(GivenAttribute), false).Length > 0)
//                      .ToArray();


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

//File.Delete(testProject.TestResultFile!);

Console.WriteLine($"Gurka file created: {outputfile}");