using SpecGurka.Exceptions;
using SpecGurka.Interfaces;
using SpecGurka.GherkinTools;
using Newtonsoft.Json;
using RestSharp;
using SpecGurka.Config;

namespace SpecGurka.WorkSystems.Github;

public class GithubUserStoryService : ISystemUserStoryService
{
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly GithubConfig githubConfig;
    private readonly GithubClient githubClient;
    public GithubUserStoryService(UIHelper UI, GherkinFileService fileService, GithubConfig githubConfig, GithubClient githubClient)
    {
        this.UI = UI;
        this.fileService = fileService;
        this.githubConfig = githubConfig;
        this.githubClient = githubClient;
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
            RestResponse response;
            response = await githubClient.RequestGithubApi(id);
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
            UI.PrintError($"User story '{id}' doesn't exist on the system.");
            return;
        }

        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(response.Content)!;

        string workItemType;
        try{
            workItemType = ExtractWorkItemTypeForUserStory(dynamicObject, id);
        }catch{
            throw;
        }

        var userStory = "userstory";
        if (workItemType != userStory)
        {
            UI.PrintError($"User story '{id}' exists on the system, but is a {workItemType}.");
            return;
        }

        //TODO: can create a really long output, maybe only print errors?
        if (response.IsSuccessful && workItemType == userStory)
            UI.PrintOk($"User story '{id}' exists on the system as a user story.");
    }

    private string ExtractWorkItemTypeForUserStory(dynamic response, string id)
    {
        List<string> workItemTypes = githubClient.ExtractAllWorkItemTypes(response);

        if (workItemTypes.Count < 1)
        {
            UI.PrintError($"User story '{id}' is missing a work item type on the system.");
            throw new NotFoundException("The work item is missing a work item type.");
        }

        if (workItemTypes.Count > 1)
        {
            UI.PrintError($"User story '{id}' has multiple work item types on the system.");
            throw new NotFoundException("The work item has multiple work item types.");
        }

        return workItemTypes[0];

    }
}
