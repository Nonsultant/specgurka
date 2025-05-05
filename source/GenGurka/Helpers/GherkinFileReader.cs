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
            throw new UnableToReadFileException($"Unable to parse or read {path}");
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
            if (gherkinDoc.Feature == null)
                continue;
            
            gherkinDocs.Add(featureFile, gherkinDoc);
        }

        return gherkinDocs;
    }
}


