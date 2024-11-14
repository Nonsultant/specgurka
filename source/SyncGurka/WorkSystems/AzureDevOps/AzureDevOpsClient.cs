using SpecGurka.Interfaces;
using Newtonsoft.Json;
using RestSharp;
using SpecGurka.GherkinTools;
using SpecGurka.Config;

namespace SpecGurka.WorkSystems.AzureDevOps;

public class AzureDevOpsClient : ISystemClient
{
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly AzureDevOpsConfig config;

    public AzureDevOpsClient(UIHelper UI, GherkinFileService fileService, AzureDevOpsConfig config)
    {
        this.UI = UI;
        this.fileService = fileService;
        this.config = config;
    }

    public async Task<WorkItem> GetWorkItemFromSystem(string workItemId)
    {
        var response = await RequestAzureDevOpsApi(workItemId);

        var workItem = ExtractWorkItemFromResponse(response);
        workItem.Id = workItemId;

        return workItem;
    }

    private WorkItem ExtractWorkItemFromResponse(RestResponse azureDevOpsApiResponse)
    {
        dynamic azureDevOpsResponse;


        try
        {
            azureDevOpsResponse = JsonConvert.DeserializeObject<dynamic>(azureDevOpsApiResponse.Content)!;
        }
        catch
        {
            UI.PrintError("Something went wrong when fetching the data from Azure DevOps");
            throw;
        }


        WorkItem workItem = new WorkItem()
        {
            Title = ExtractWorkItemTitle(azureDevOpsResponse),
            ItemType = ExtractWorkItemType(azureDevOpsResponse),
            SpecflowStatus = ExtractSpecflowStatus(azureDevOpsResponse),
            FileLink = ExtractFileLink(azureDevOpsResponse)
        };

        UI.PrintOk("The feature exists on Azure.");

        return workItem;
    }

    public string ExtractWorkItemType(dynamic dynamicObject)
    {
        string workItemType;

        try
        {
            workItemType = (dynamicObject.value[0].fields["System.WorkItemType"]).ToString().ToLower();
        }
        catch
        {
            UI.PrintError("The work item type could not be retrieved from Azure.");
            throw;
        }

        return workItemType;
    }

    private string ExtractWorkItemTitle(dynamic dynamicObject)
    {
        string workItemName;

        try
        {
            workItemName = (dynamicObject.value[0].fields["System.Title"]).ToString();
        }
        catch
        {
            UI.PrintError("A feature with that Id does not exist on Azure.");
            throw;
        }

        return workItemName;
    }

    private string ExtractSpecflowStatus(dynamic response)
    {
        string specflowStatus = null;
        string specflowField = config.SpecflowStatusId;

        try
        {
            specflowStatus = (response.value[0].fields[specflowField]).ToString();
        }
        catch
        {
            UI.PrintWarning("The specflow status could not be retrieved from Azure.");
        }

        return specflowStatus;
    }

    private string ExtractFileLink(dynamic response)
    {
        string fileLink = null;
        string fileLinkId = config.FileLinkId;

        try
        {
            fileLink = (response.value[0].fields[fileLinkId]).ToString();
        }
        catch
        {
            UI.PrintWarning("The file link could not be retrieved from Azure.");
        }

        return fileLink;
    }

    public async Task<RestResponse> RequestAzureDevOpsApi(string workItemId)
    {
        var url = GetAzureWorkItemUrl(workItemId);

        var client = new RestClient();

        string authToken = config.AuthToken;

        var request = new RestRequest(url);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", authToken);

        RestResponse response;

        try
        {
            response = await client.GetAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong fetching data from Azure.");
            throw;
        }

        return response;
    }

    public string GetAzureWorkItemUrl(string id)
    {
        string organization = config.Owner;
        string project = config.Project;
        string baseUrl = config.BaseUrl;

        string url = $"{baseUrl}/{organization}/{project}/_apis/wit/workitems?ids={id}&api-version=7.1-preview.3";

        return url;
    }

    public string GetAzureDevOpsUpdateWorkItemUrl(string id)
    {
        string organization = config.Owner;
        string project = config.Project;
        string baseUrl = config.BaseUrl;

        var url = $"{baseUrl}/{organization}/{project}/_apis/wit/workitems/{id}?api-version=7.0";

        return url;
    }
}