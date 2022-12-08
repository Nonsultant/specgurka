using SpecGurka.Exceptions;

namespace SpecGurka.GherkinTools;

public class GherkinFolderReader
{
    private readonly UIHelper UI;

    public GherkinFolderReader(UIHelper UI)
    {
        this.UI = UI;
    }

    public string[] GetGherkinFilesInFolder(string path)
    {
        var folderName = Path.GetFileName(path);
        UI.PrintReading("FOLDER", folderName);

        if (Directory.Exists(path))
        {
            var gherkinFiles = ReadGherkinFolder(path);
            return gherkinFiles;
        }

        else
        {
            UI.PrintError("The given directory could not be found.");
            throw new NotFoundException("The directory was not found.");
        }

    }

    public string[] ReadGherkinFolder(string path)
    {
        return Directory.GetFiles(path, "*.feature", SearchOption.AllDirectories);
    }
}
