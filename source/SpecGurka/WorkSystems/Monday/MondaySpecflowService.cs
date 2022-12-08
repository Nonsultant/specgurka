using SpecGurka.Config;
using SpecGurka.Interfaces;
using SpecGurka.Specflow;
using RestSharp;

namespace SpecGurka.WorkSystems.Monday;

public class MondaySpecflowService : ISystemSpecflowService
{
    private readonly UIHelper UI;
    private readonly MondayClient mondayClient;
    private readonly MondayConfig mondayConfig;

    public MondaySpecflowService(UIHelper UI, MondayClient mondayClient, MondayConfig mondayConfig)
    {
        this.UI = UI;
        this.mondayClient = mondayClient;
        this.mondayConfig = mondayConfig;
    }

    public async Task UpdateSystemWithSpecflowResults(WorkItem serviceFeatureItem, SpecflowFeatureResult specflowResult)
    {
        var body = GetSpecflowUpdateQuery(specflowResult.AllScenariosAreOK(), serviceFeatureItem.Id);
        RestResponse response;

        try
        {
            response = await mondayClient.RequestMondayApi(body);
        }
        catch
        {
            UI.PrintError("Something went wrong when updating the specflow result on Monday.");
            throw;
        }

        VerifySpecflowUpdate(response, serviceFeatureItem.Title);
    }

    private string GetSpecflowUpdateQuery(bool allScenariosOK, string workId)
    {
        string boardId = mondayConfig.FeatureBoardId;
        string specflowStatusColumnId = mondayConfig.SpecflowStatusId;

        if (allScenariosOK)
        {
            return $"mutation {{change_simple_column_value(board_id: {boardId}, item_id: {workId}, column_id: \"{specflowStatusColumnId}\", value: \"OK\") {{id name board {{ id name}} }} }}";
        }

        else
        {
            return $"mutation {{change_simple_column_value(board_id: {boardId}, item_id: {workId}, column_id: \"{specflowStatusColumnId}\", value: \"NOK\") {{id name board {{ id name}} }} }}";
        }
    }

    private void VerifySpecflowUpdate(RestResponse response, string itemName)
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
