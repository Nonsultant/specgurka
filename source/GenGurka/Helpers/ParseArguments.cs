namespace SpecGurka.GenGurka.Helpers;

public static class Arguments
{
    public static Dictionary<string, string> ToDictionary(string[] args)
    {
        var arguments = new Dictionary<string, string>();

        if (args.Length % 2 != 0)
        {
            throw new ArgumentException("Arguments should be in key-value pairs.");
        }

        for (int i = 0; i < args.Length; i += 2)
        {
            string key = args[i];
            string value = args[i + 1];
            arguments[key] = value;
        }

        return arguments;
    }
}