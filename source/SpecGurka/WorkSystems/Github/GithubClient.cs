using SpecGurka.Config;
using SpecGurka.Exceptions;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace SpecGurka.WorkSystems.Github;

public class GithubClient : ISystemClient
{
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly GithubConfig githubConfig;
    public GithubClient(UIHelper UI, GherkinFileService fileService, GithubConfig githubConfig)
    {
        this.UI = UI;
        this.fileService = fileService;
        this.githubConfig = githubConfig;
    }

    public async Task<WorkItem> GetWorkItemFromSystem(string workItemId)
    {
        var response = await RequestGithubApi(workItemId);

        var workItem = ExtractWorkItemFromResponse(response);
        workItem.Id = workItemId;

        return workItem;
    }

    private WorkItem ExtractWorkItemFromResponse(RestResponse apiResponse)
    {
        var response = JsonConvert.DeserializeObject<dynamic>(apiResponse.Content)!;

        TestThatWorkItemExists(response);

        WorkItem workItem = new WorkItem()
        {
            Title = ExtractWorkItemTitle(response),
            ItemType = ExtractWorkItemTypeForFeature(response),
            SpecflowStatus = ExtractSpecflowStatus(response),
            FileLink = ExtractWorkItemFileLink(response)
        };

        return workItem;
    }

    private void TestThatWorkItemExists(dynamic response)
    {
        if (response.title == null)
        {
            UI.PrintError("An issue with that Id does not exist on Github.");
            throw new NotFoundException("An issue with that Id does not exist on Github.");
        }

        UI.PrintOk("The feature exists on Github.");
    }

    private string ExtractWorkItemTypeForFeature(dynamic response)
    {
        List<string> workItemTypes = ExtractAllWorkItemTypes(response);

        if (workItemTypes.Count < 1)
        {
            UI.PrintError("The issue is missing a work item type.");
            throw new NotFoundException("The work item is missing a work item type.");
        }

        if (workItemTypes.Count > 1)
        {
            UI.PrintError("The issue has multiple work item types.");
            throw new NotFoundException("The work item has multiple work item types.");
        }

        return workItemTypes[0];

    }

    public List<string> ExtractAllWorkItemTypes(dynamic response)
    {
        List<string> workItemTypes = new();

        foreach (var label in response.labels)
        {
            var workItemType = label.name.ToString().ToLower();

            if (workItemType == "feature" || workItemType == "userstory")
            {
                workItemTypes.Add(workItemType);
            }
        }

        return workItemTypes;
    }

    private string ExtractWorkItemTitle(dynamic dynamicObject)
    {
        string workItemName;

        try
        {
            workItemName = dynamicObject.title;
        }
        catch
        {
            UI.PrintError("An issue with that Id does not exist on Github.");
            throw;
        }

        return workItemName;
    }

    private string ExtractSpecflowStatus(dynamic response)
    {
        List<string> specflowStatuses = ExtractAllSpecflowStatuses(response);

        if (specflowStatuses.Count > 1)
        {
            UI.PrintError("The issue has multiple specflow statues.");
            throw new NotFoundException("The work item has specflow statuses.");
        }

        return specflowStatuses[0];
    }

    private List<string> ExtractAllSpecflowStatuses(dynamic response)
    {
        List<string> specflowStatuses = new();

        foreach (var label in response.labels)
        {
            var specflowStatus = label.name.ToString().ToLower();

            if (specflowStatus == "specflow-ok")
                specflowStatuses.Add("OK");

            if (specflowStatus == "specflow-nok")
                specflowStatuses.Add("NOK");
        }

        return specflowStatuses;
    }

    private string ExtractWorkItemFileLink(dynamic dynamicObject)
    {
        string workItemLink;

        try
        {
            workItemLink = dynamicObject.body;
        }
        catch
        {
            UI.PrintError("The file link could not be retrieved from Github.");
            throw;
        }

        return workItemLink;
    }

    public async Task<RestResponse> RequestGithubApi(string workItemId)
    {
        var url = GetGithubUrl(workItemId);

        var client = new RestClient();

        var request = new RestRequest(url);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", githubConfig.AuthToken);

        RestResponse response;

        try
        {
            response = await client.GetAsync(request);
        }
        catch
        {
            UI.PrintError("Something went wrong fetching data from Github.");
            throw;
        }

        return response;
    }

    public string GetGithubUrl(string id)
    {
        string baseUrl = githubConfig.BaseUrl;
        string owner = githubConfig.Owner;
        string repo = githubConfig.Project;

        string url = $"{baseUrl}/{owner}/{repo}/issues/{id}";

        return url;
    }
}