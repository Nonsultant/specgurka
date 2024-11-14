using SpecGurka.Exceptions;
using Gherkin.Ast;

namespace SpecGurka.GherkinTools;

public class GherkinFileReader
{
    private readonly UIHelper UI;

    public GherkinFileReader(UIHelper UI)
    {
        this.UI = UI;
    }

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
            UI.PrintError("The gherkin file could not be read.");
            throw new UnableToReadFileException("The file could not be read.");
        }

        if (gherkinDoc.Feature == null)
        {
            UI.PrintError("The gherkin file could not be read.");
            throw new UnableToReadFileException("The file could not be read.");
        }
        return gherkinDoc;
    }
}


