using Gherkin.Ast;
using SpecGurka.GenGurka.Exceptions;

namespace SpecGurka.GenGurka.Helpers;

public static class GherkinFileReader
{

    public static GherkinDocument ReadGherkinFile(string path)
    {
        var parser = new Gherkin.Parser();
        GherkinDocument gherkinDoc;

        try
        {
            gherkinDoc = parser.Parse(path);
        }
        catch
        {
            //UI.PrintError("The gherkin file could not be read.");
            throw new UnableToReadFileException("The file could not be read.");
        }

        if (gherkinDoc.Feature == null)
        {
            // UI.PrintError("The gherkin file could not be read.");
            throw new UnableToReadFileException("The file could not be read.");
        }
        return gherkinDoc;
    }

    public static Dictionary<string, GherkinDocument> ReadFiles(string directoryPath)
    {
        var featureFiles = Directory.GetFiles(directoryPath, "*.feature", SearchOption.AllDirectories);
        var gherkinDocs = new Dictionary<string, GherkinDocument>();

        foreach (var featureFile in featureFiles)
        {
            var gherkinDoc = ReadGherkinFile(featureFile);
            gherkinDocs.Add(featureFile, gherkinDoc);
        }

        return gherkinDocs;
    }
}


