using SpecGurka.Config;
using SpecGurka.Interfaces;
using SpecGurka.Specflow;
using RestSharp;

namespace SpecGurka.WorkSystems.AzureDevOps;

public class AzureDevOpsSpecflowService : ISystemSpecflowService
{
    private readonly UIHelper UI;
    private readonly AzureDevOpsConfig config;
    private readonly AzureDevOpsClient azureClient;

    public AzureDevOpsSpecflowService(AzureDevOpsConfig config, UIHelper UI, AzureDevOpsClient azureClient)
    {
        this.config = config;
        this.UI = UI;
        this.azureClient = azureClient;
    }
    public async Task UpdateSystemWithSpecflowResults(WorkItem serviceFeatureItem, SpecflowFeatureResult specflowResult)
    {
        var url = azureClient.GetAzureDevOpsUpdateWorkItemUrl(serviceFeatureItem.Id);
        var request = new RestRequest(url);

        var authToken = config.AuthToken;

        request.AddHeader("Content-Type", "application/json-patch+json");
        request.AddHeader("Authorization", authToken);

        var body = GetSpecflowUpdateBody(specflowResult.AllScenariosAreOK());
        request.AddBody(body);

        var client = new RestClient();
        RestResponse response;

        try
        {
            response = await client.PatchAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong when updating Azure with the specflow results.");
            throw;
        }

        await PostSpecflowResultCommentOnAzureDevOps(serviceFeatureItem, specflowResult.AllScenariosAreOK());
        VerifySpecflowFieldUpdate(response, serviceFeatureItem.Title);
    }
    

    private string GetSpecflowUpdateBody(bool allScenariosOK)
    {
        var specflowField = config.SpecflowStatusId;

        if (allScenariosOK)
        {
            return "[{" +
                "\"op\": \"replace\"," +
                $"\"path\": \"/fields/{specflowField}\"," +
                "\"value\": \"OK\"" +
                "}]";
        }

        else
        {
            return "[{" +
                "\"op\": \"replace\"," +
                $"\"path\": \"/fields/{specflowField}\"," +
                "\"value\": \"NOK\"" +
                "}]";
        }
    }

    private async Task PostSpecflowResultCommentOnAzureDevOps(WorkItem serviceItem, bool specflowResultIsOK)
    {
        var url = GetAzureDevOpsPostCommentUrl(serviceItem.Id);
        var request = new RestRequest(url);

        var authToken = config.AuthToken;

        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", authToken);

        var comment = GetAzureDevOpsSpecflowComment(specflowResultIsOK, serviceItem.SpecflowStatus);
        var body = $"{{\"text\" : \"{comment}\"}}";
        request.AddBody(body);

        var client = new RestClient();
        RestResponse response;

        try
        {
            response = await client.PostAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong when commenting on Azure.");
            throw;
        }
    }

    private string GetAzureDevOpsPostCommentUrl(string id)
    {
        string organization = config.Owner;
        string project = config.Project;
        string baseUrl = config.BaseUrl;

        string url = $"{baseUrl}/{organization}/{project}/_apis/wit/workItems/{id}/comments?api-version=6.0-preview.3";

        return url;
    }

    private string GetAzureDevOpsSpecflowComment(bool specflowResultIsOK, string azureSpecflowStatus)
    {
        if (specflowResultIsOK)
            return $"The specflow result was updated from {azureSpecflowStatus} to OK.";
        else
            return $"The specflow result was updated from {azureSpecflowStatus} to NOK";
    }
    private void VerifySpecflowFieldUpdate(RestResponse response, string itemName)
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
}
