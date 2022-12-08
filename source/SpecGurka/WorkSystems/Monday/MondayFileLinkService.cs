using SpecGurka.Config;  
using SpecGurka.Exceptions;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using RestSharp;

namespace SpecGurka.WorkSystems.Monday;

public class MondayFileLinkService : ISystemFileLinkService
{
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly MondayClient mondayClient;
    private readonly MondayConfig mondayConfig;

    public MondayFileLinkService(UIHelper UI, GherkinFileService fileService, MondayClient mondayClient, MondayConfig mondayConfig)
    {
        this.UI = UI;
        this.fileService = fileService;
        this.mondayClient = mondayClient;
        this.mondayConfig = mondayConfig;
    }

    public async Task UpdateSystemFileLink(string fileLink, Feature feature)
    {
        string body = GetFileLinkUpdateQuery(fileLink, feature.ServiceFeatureItem.Id);
        RestResponse response;

        try
        {
            response = await mondayClient.RequestMondayApi(body);
        }
        catch
        {
            UI.PrintError("Something went wrong when updating the file link on Monday.");
            throw;
        }

        VerifyGherkinFileLinkUpdate(response, feature.ServiceFeatureItem.Id);
    }

    private void VerifyGherkinFileLinkUpdate(RestResponse response, string itemId)
    {
        if (response.IsSuccessful)
        {
            UI.PrintOk($"Work item '{itemId}'s gherkin file link was successfully updated.");
        }

        else
        {
            UI.PrintError($"Work item '{itemId}'s gherkin file link could not be updated.");
        }
    }

    private string GetFileLinkUpdateQuery(string fileLink, string workId)
    {
        string query = $"mutation {{ change_simple_column_value (board_id: {mondayConfig.FeatureBoardId}, item_id: {workId}, " +
                        $"column_id: \"{mondayConfig.FileLinkId}\", value: \"{fileLink}\") {{ id name board {{id name}} }} }}";
        return query;
    }

    public void VerifyGherkinFileLinkOnSystem(string filePath, WorkItem serviceWorkItem)
    {
        var gherkinFileLink = fileService.CreateGherkinFileLink(filePath);

        if (gherkinFileLink == serviceWorkItem.FileLink)
        {
            UI.PrintOk("The gherkin file link is correct.");
            return;
        }
        else
        {
            UI.PrintError("The gherkin file link doesn't exist on Monday.");
            throw new WrongFeatureConfigurationException();
        }
    }

    public string CleanSystemFileLink(string gherkinFilePath, string fileLink)
    {
        var cleanFilePath = gherkinFilePath.Replace("../", "").Replace("\\", "/");
        var cleanFileLink = fileLink.Replace("<a href=", "")
            .Replace("</a>", "")
            .Replace("\"", "")
            .Replace("> " + cleanFilePath, "");

        return cleanFileLink;
    }
}
