#r "System.Xml"
#r "System.Xml.Linq"

using System.Xml;
using System.Xml.Linq;

var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");
var buildId = Argument("buildId", "0");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .WithCriteria(c => HasArgument("rebuild"))
    .Does(() =>
{
    CleanDirectory($"./source/GenGurka/bin/{configuration}");
    CleanDirectory($"./source/GurkaSpec/bin/{configuration}");
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild("./GenGurka.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
    });
});

Task("Pack NuGet tool")
    .IsDependentOn("Build")
        .Does(() =>
{
    var projectFile = "./source/GenGurka/GenGurka.csproj";

    // Load the .csproj file as XML
    var xml = XElement.Load(projectFile);

    // Find or create the <Version> element
    var versionElement = xml.Descendants("Version").FirstOrDefault();
    if (versionElement == null)
    {
        versionElement = new XElement("Version", $"1.0.{buildId}"); // Default version
        xml.Add(versionElement);
    }
    else
    {
        versionElement.Value = $"1.0.{buildId}"; // New version value
    }

    // Save the changes
    xml.Save(projectFile);

    DotNetPack("source\\GenGurka\\GenGurka.csproj");
});
// Task("Test")
//     .IsDependentOn("Build")
//     .Does(() =>
// {
//     DotNetTest("./src/Example.sln", new DotNetTestSettings
//     {
//         Configuration = configuration,
//         NoBuild = true,
//     });
// });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
