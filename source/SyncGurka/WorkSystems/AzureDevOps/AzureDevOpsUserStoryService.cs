using SpecGurka.Config;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace SpecGurka.WorkSystems.AzureDevOps;

public class AzureDevOpsUserStoryService : ISystemUserStoryService
{
    private readonly AzureDevOpsConfig config;
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly AzureDevOpsClient devOpsClient;

    public AzureDevOpsUserStoryService(AzureDevOpsConfig config, UIHelper UI, GherkinFileService fileService, AzureDevOpsClient devOpsClient)
    {
        this.config = config;
        this.UI = UI;
        this.fileService = fileService;
        this.devOpsClient = devOpsClient;
    }


    private string GetAzureWorkItemUrl(string id)
    {
        string organization = config.Owner;
        string project = config.Project;

        string url = $"{config.BaseUrl}/{organization}/{project}/_apis/wit/workitems?ids={id}&api-version=7.1-preview.3";

        return url;
    }

    public async Task TestIfUserStoriesExistOnSystem(List<Feature> features)
    {
        List<string> allIds = new();

        foreach (var feature in features)
        {
            allIds.AddRange(fileService.GetAllUserStoryIdsOnScenarioLevel(feature.GherkinFileContent));
            allIds.AddRange(fileService.GetAllUserStoryIdsOnFeatureLevel(feature.GherkinFileContent));
        }

        var ids = fileService.RemoveDuplicatedIds(allIds);

        foreach (var id in ids)
        {
            var response = await devOpsClient.RequestAzureDevOpsApi(id);

            try
            {
                TestUserStoryOnSystem(response, id);
            }
            catch
            {
                //error message already printed
            }
        }
    }

    private void TestUserStoryOnSystem(RestResponse response, string id)
    {
        if (!response.IsSuccessful)
        {
            UI.PrintError($"User story '{id}' doesn't exist on the service.");
            return;
        }

        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(response.Content)!;
        var workItemType = devOpsClient.ExtractWorkItemType(dynamicObject);

        if (workItemType != "user story")
        {
            UI.PrintError($"User story '{id}' exists on the service, but is a {workItemType}.");
            return;
        }

        //TODO: can create a really long output, maybe only print errors?
        if (workItemType == "user story")
            UI.PrintOk($"User story '{id}' exists on the service as a user story.");
    }
}
