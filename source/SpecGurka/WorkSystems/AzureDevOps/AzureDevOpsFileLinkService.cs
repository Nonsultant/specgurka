using SpecGurka.Config;
using SpecGurka.Exceptions;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using RestSharp;


namespace SpecGurka.WorkSystems.AzureDevOps;

public class AzureDevOpsFileLinkService : ISystemFileLinkService
{
    private readonly AzureDevOpsConfig config;
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly AzureDevOpsClient azureClient;

    public AzureDevOpsFileLinkService(AzureDevOpsConfig config, UIHelper UI, GherkinFileService fileService, 
        AzureDevOpsClient azureClient)
    {
        this.config = config;
        this.UI = UI;
        this.fileService = fileService;
        this.azureClient = azureClient;
    }

    public async Task UpdateSystemFileLink(string fileLink, Feature feature)
    {
        var url = azureClient.GetAzureDevOpsUpdateWorkItemUrl(feature.ServiceFeatureItem.Id);
        var request = new RestRequest(url);

        var authToken = config.AuthToken;

        request.AddHeader("Content-Type", "application/json-patch+json");
        request.AddHeader("Authorization", authToken);

        var fileLinkField = config.FileLinkId;
        var cleanFilePath = feature.GherkinFilePath.Replace("../", "").Replace("\\", "/");

        var body = "[{" +
                "\"op\": \"replace\"," +
                $"\"path\": \"/fields/{fileLinkField}\"," +
                $"\"value\": \"<a href={fileLink}> {cleanFilePath}</a>\"" +
                "}]";

        request.AddBody(body);

        var client = new RestClient();
        RestResponse response;

        try
        {
            response = await client.PatchAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong when updating Azure with the file link.");
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

    public void VerifyGherkinFileLinkOnSystem(string filePath, WorkItem serviceWorkItem)
    {
        var gherkinFileLink = fileService.CreateGherkinFileLink(filePath);
        var systemFileLink = CleanSystemFileLink(filePath, serviceWorkItem.FileLink);

        if (gherkinFileLink == systemFileLink)
        {
            UI.PrintOk("The gherkin file link is correct.");
            return;
        }

        else
        {
            UI.PrintError("The gherkin file link doesn't exist on Azure.");
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
