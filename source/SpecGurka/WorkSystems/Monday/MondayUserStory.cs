using SpecGurka.Config;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace SpecGurka.WorkSystems.Monday;

public  class MondayUserStoryService : ISystemUserStoryService
{
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly MondayClient mondayClient;
    private readonly MondayConfig mondayConfig;

    public MondayUserStoryService(UIHelper UI, GherkinFileService fileService, MondayClient mondayClient, MondayConfig mondayConfig)
    {
        this.UI = UI;
        this.fileService = fileService;
        this.mondayClient = mondayClient;
        this.mondayConfig = mondayConfig;
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
        var specflowStatusId = mondayConfig.SpecflowStatusId;
        var fileLinkId = mondayConfig.FileLinkId;

        foreach (var id in ids)
        {
            RestResponse response; ;
            string body = $"query {{ items (ids: {id}) {{ id name column_values(ids: [ \"{specflowStatusId}\", \"{fileLinkId}\" ]) {{ text }} board {{ id name }} }} }}";

            try
            {
                response = await mondayClient.RequestMondayApi(body);
            }
            catch
            {
                UI.PrintError("Something went wrong when fetching data from Monday.");
                continue;
            }

            try
            {
                TestUserStoryOnService(response, id);
            }
            catch
            {
                continue;
                //error message already printed
            }
        }
    }

    private void TestUserStoryOnService(RestResponse response, string id)
    {
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(response.Content)!;
        string workItemType;

        try
        {
            workItemType = mondayClient.ExtractWorkItemType(dynamicObject);
        }
        catch
        {
            UI.PrintError($"User story '{id}' doesn't exist on the service.");
            throw;
        }

        var userStory = mondayConfig.UserStoryBoardId;

        if (workItemType != userStory)
        {
            UI.PrintError($"User story '{id}' exists on the service, is a {workItemType}");
            return;
        }

        //TODO: can create a really long output, maybe only print errors?
        if (response.IsSuccessful && workItemType == userStory)
            UI.PrintOk($"User story '{id}' exists on the service in the user story board.");
    }
}
