
using Gherkin.Ast;
using SpecGurka.GenGurka.Exceptions;



namespace SpecGurka.GenGurka;

public class GherkinFileReader
{

    public GherkinDocument ReadGherkinFile(string path)
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
}


