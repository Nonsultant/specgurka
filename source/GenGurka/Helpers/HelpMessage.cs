namespace SpecGurka.GenGurka.Helpers;

public static class HelpMessage
{
    public static void Show()
    {
        Console.WriteLine("Welcome to the Gurka Generator");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("-trx <path>:                             Path to the trx file from the dotnet test command.");
        Console.WriteLine("-o <path> or --output-path <path>:       Path to the output file of the .gurka file.");
        Console.WriteLine("                                         Default is the current directory");
        Console.WriteLine("-f <path> or --feature-directory <path>: Path to the directory containing the feature files");
        Console.WriteLine("                                         Default is the Features directory in the current directory");
        Console.WriteLine("-p <name> or --project-name <name>:      Name of the project the result is created from");
        Console.WriteLine("-a or --assembly <path>:                 Path to the test assembly file");
        Environment.Exit(0);
    }
}