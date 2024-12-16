namespace SpecGurka.GenGurka.Extensions;

internal static class TestProjectExtensions
{
    internal static void ApplyArgumentConfiguration(this TestProject testProject, Dictionary<string, string> arguments)
    {
        foreach (var argument in arguments)
        {
            switch (argument.Key)
            {
                case "-trx":
                    testProject.TestResultFile = argument.Value;
                    break;

                case "-o":
                case "--output-path":
                    testProject.OutputPath = argument.Value;
                    break;

                case "-f":
                case "--feature-directory":
                    testProject.FeaturesDirectory = argument.Value;
                    break;

                case "-a":
                case "--assembly":
                    testProject.AssemblyFile = argument.Value;
                    break;

                case "-p":
                case "--project-name":
                    testProject.ProjectName = argument.Value;
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {argument.Key}");
            }
        }

        // Default values
        testProject.FeaturesDirectory ??= Path.Combine(Directory.GetCurrentDirectory(), "Features");
        testProject.TestResultFile ??= Directory.GetFiles(Directory.GetCurrentDirectory(), "*.trx").First();
        testProject.OutputPath ??= $"{Directory.GetCurrentDirectory()}/";
    }
}