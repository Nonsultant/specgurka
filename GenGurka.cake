var target = Argument("target", "Build");
var configuration = Argument("configuration", "Release");

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

Task("Build GenGurka")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetBuild("./GenGurka.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
    });
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
