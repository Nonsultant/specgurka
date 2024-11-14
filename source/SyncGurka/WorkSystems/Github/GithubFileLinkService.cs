using SpecGurka.Config;
using SpecGurka.Exceptions;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using RestSharp;

namespace SpecGurka.WorkSystems.Github;

public class GithubFileLinkService : ISystemFileLinkService
{
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly GithubConfig githubConfig;
    private readonly GithubClient githubClient;

    public GithubFileLinkService(UIHelper UI, GherkinFileService fileService, GithubConfig githubConfig, GithubClient githubClient)
    {
        this.UI = UI;
        this.fileService = fileService;
        this.githubConfig = githubConfig;
        this.githubClient = githubClient;
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
            UI.PrintError("The gherkin file link doesn't exist on Github.");
            throw new WrongFeatureConfigurationException();
        }
    }

    public async Task UpdateSystemFileLink(string fileLink, Feature feature)
    {
        var url = githubClient.GetGithubUrl(feature.ServiceFeatureItem.Id);
        var request = new RestRequest(url);

        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", githubConfig.AuthToken);
        request.AddHeader("Accept", "application/vnd.github+json");

        var body = new
        {
            body = fileLink
        };

        request.AddJsonBody(body);

        var client = new RestClient();
        RestResponse response;

        try
        {
            response = await client.PostAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong when updating the file link on Github.");
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
