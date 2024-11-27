namespace SpecGurka.GenGurka.Helpers;

public static class HelpMessage
{
    public static void Show()
    {
        Console.WriteLine("Welcome to the Gurka Generator");
        Console.WriteLine();
        Console.WriteLine("Argument to specify:");
        Console.WriteLine("-trx <path>:                      Path to the trx file from the dotnet test command");
        Console.WriteLine("-o or --output-path <path>:       Path to the output file of the .gurka file, default is current directory");
        Console.WriteLine("-f or --feature-directory <path>: Path to the directory containing the feature files");
        Console.WriteLine("-a or --assembly <path>:          Path to the test assembly file");
        Environment.Exit(0);
    }
}