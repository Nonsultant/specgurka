﻿using SpecGurka.GenGurka;
using TrxFileParser.Models;
using SpecGurka.GurkaSpec;
using System.Globalization;
using Gherkin.Ast;
using SpecGurka.GenGurka.Extensions;
using SpecGurka.GenGurka.Helpers;
using Feature = SpecGurka.GurkaSpec.Feature;

var arguments = Arguments.ToDictionary(args);
Console.WriteLine("Starting generation of Gurka file...");

var gurka = new Testrun
{
    Name = "DemoProject",
    Date = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
};

var gurkaProject = new Product { Name = "Company Management" };
gurka.Products.Add(gurkaProject);

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