using SpecGurka.Specflow;
using SpecGurka.Interfaces;
using RestSharp;
using SpecGurka.Config;

namespace SpecGurka.WorkSystems.Github;

public class GithubSpecflowService : ISystemSpecflowService
{
    private readonly UIHelper UI;
    private readonly GithubConfig githubConfig;

    public GithubSpecflowService(UIHelper UI, GithubConfig githubConfig)
    {
        this.UI = UI;
        this.githubConfig = githubConfig;
    }

    public async Task UpdateSystemWithSpecflowResults(WorkItem serviceFeatureItem, SpecflowFeatureResult specflowResult)
    {
        var url = GetGithubLabelUrl(serviceFeatureItem.Id);

        var client = new RestClient();

        var request = new RestRequest(url);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", githubConfig.AuthToken);

        var body = GetGithubUpdateBody(specflowResult.AllScenariosAreOK());

        request.AddBody(body);

        RestResponse response;

        try
        {
            response = await client.PostAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong when updating the Github with the specflow results.");
            throw;
        }

        await RemoveOldGithubTag(serviceFeatureItem.Id, specflowResult.AllScenariosAreOK());
        await PostSpecflowResultCommentOnGithub(serviceFeatureItem, specflowResult.AllScenariosAreOK());
        VerifySpecflowTagUpdate(response, serviceFeatureItem.Title);

    }

    private string GetGithubLabelUrl(string id)
    {
        string baseUrl = githubConfig.BaseUrl;
        string owner = githubConfig.Owner;
        string repo = githubConfig.Project;

        string url = $"{baseUrl}/{owner}/{repo}/issues/{id}/labels";

        return url;
    }

    private string GetGithubUpdateBody(bool allScenariosOK)
    {
        if (allScenariosOK)
        {
            return "{\"labels\":[\"specflow-ok\"]}";
        }

        else
        {
            return "{\"labels\":[\"specflow-nok\"]}";
        }
    }

    private string GetGithubTagToRemove(bool allScenariosOK)
    {
        if (!allScenariosOK)
        {
            return "specflow-ok";
        }

        else
        {
            return "specflow-nok";
        }
    }

    private async Task RemoveOldGithubTag(string featureId, bool allScenariosOk)
    {
        var tagToRemove = GetGithubTagToRemove(allScenariosOk);

        var url = GetGithubLabelUrl(featureId) + "/" + tagToRemove;

        var client = new RestClient();


        var request = new RestRequest(url);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", githubConfig.AuthToken);

        RestResponse response;

        response = await client.DeleteAsync(request);

        if (!response.IsSuccessful)
        {
            UI.PrintError($"There was an issue deleting the old specflow tag on issue {featureId} on Github.");
        }

    }

    private void VerifySpecflowTagUpdate(RestResponse response, string itemName)
    {
        if (response.IsSuccessful)
        {
            UI.PrintOk($"'{itemName}'s specflow test status was successfully updated.");
        }

        else
        {
            UI.PrintError($"'{itemName}'s specflow test status could not be updated.");
        }
    }

    private async Task PostSpecflowResultCommentOnGithub(WorkItem serviceItem, bool specflowResultIsOK)
    {
        var url = GetGithubPostCommentUrl(serviceItem.Id);
        var request = new RestRequest(url);

        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", githubConfig.AuthToken);
        request.AddHeader("Accept", "application/vnd.github+json");

        var comment = GetGithubSpecflowComment(specflowResultIsOK, serviceItem.SpecflowStatus);
        var body = $"{{\"body\" : \"{comment}\"}}";
        request.AddBody(body);

        var client = new RestClient();
        RestResponse response;

        try
        {
            response = await client.PostAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong when commenting on Github.");
            throw;
        }
    }

    private string GetGithubPostCommentUrl(string id)
    {
        string baseUrl = githubConfig.BaseUrl;
        string owner = githubConfig.Owner;
        string repo = githubConfig.Project;

        string url = $"{baseUrl}/{owner}/{repo}/issues/{id}/comments";

        return url;
    }

    private string GetGithubSpecflowComment(bool specflowResultIsOK, string githubSpecflowStatus)
    {
        if (specflowResultIsOK)
            return $"The specflow result was updated from {githubSpecflowStatus} to OK.";
        else
            return $"The specflow result was updated from {githubSpecflowStatus} to NOK";
    }
}
