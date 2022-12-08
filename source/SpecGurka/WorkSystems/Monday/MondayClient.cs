using SpecGurka.Config;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace SpecGurka.WorkSystems.Monday;

public class MondayClient : ISystemClient
{
    private readonly UIHelper UI;
    private readonly GherkinFileService fileService;
    private readonly MondayConfig mondayConfig;

    public MondayClient(UIHelper UI, GherkinFileService fileService, MondayConfig mondayConfig)
    {

        this.UI = UI;
        this.fileService = fileService; 
        this.mondayConfig = mondayConfig;
    }

    public async Task<WorkItem> GetWorkItemFromSystem(string workItemId)
    {            
        string body = $"query {{ items (ids: {workItemId}) {{ id name column_values(ids: [ \"{mondayConfig.SpecflowStatusId}\", \"{mondayConfig.FileLinkId}\" ]) {{ text }} board {{ id name }} }} }}";
        RestResponse response;

        try
        {
            response = await RequestMondayApi(body);
        }
        catch
        {
            UI.PrintError("Something went wrong when fetching data from Monday.");
            throw;
        }

        var workItem = ExtractWorkItemFromResponse(response);

        workItem.Id = workItemId;
        return workItem;
    }

    private WorkItem ExtractWorkItemFromResponse(RestResponse mondayApiResponse)
    {
        var dynamicObject = JsonConvert.DeserializeObject<dynamic>(mondayApiResponse.Content)!;

        WorkItem workItem = new WorkItem()
        {
            //these are tested later
            Title = ExtractWorkItemTitle(dynamicObject),
            ItemType = ExtractWorkItemType(dynamicObject),
            SpecflowStatus = ExtractWorkItemSpecflowStatus(dynamicObject),
            FileLink = ExtractWorkItemLink(dynamicObject)
        };

        return workItem;
    }

    public string ExtractWorkItemType(dynamic dynamicObject)
    {
        string workItemType;
        workItemType = (dynamicObject.data.items[0]["board"]["name"]).ToString().ToLower();

        return workItemType;
    }

    private string ExtractWorkItemTitle(dynamic dynamicObject)
    {
        string workItemName;

        try
        {
            workItemName = (dynamicObject.data.items[0]["name"]).ToString();
        }
        catch
        {
            UI.PrintError("A feature with that Id does not exist on Monday.");
            throw;
        }

        return workItemName;
    }
    private string ExtractWorkItemSpecflowStatus(dynamic dynamicObject)
    {
        string workItemSpecflowStatus;

        try
        {
            workItemSpecflowStatus = (dynamicObject.data.items[0]["column_values"][0]["text"]).ToString();
        }
        catch
        {
            UI.PrintError("The work item specflow status could not be retrieved from Monday.");
            throw;
        }

        return workItemSpecflowStatus;
    }

    private string ExtractWorkItemLink(dynamic dynamicObject)
    {
        string workItemLink;
        try
        {
            workItemLink = (dynamicObject.data.items[0]["column_values"][1]["text"]).ToString();
        }
        catch
        {
            UI.PrintError("The git link could not be retrieved from Monday.");
            throw;
        }

        return workItemLink;
    }

    public async Task<RestResponse> RequestMondayApi(string body)
    {
        var client = new RestClient();

        var request = new RestRequest(mondayConfig.BaseUrl);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", mondayConfig.AuthToken);
        request.AddJsonBody(new
        {
            query = body
        });

        RestResponse response;

        response = await client.PostAsync(request);

        return response;
    }
}