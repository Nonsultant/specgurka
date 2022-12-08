using SpecGurka.Config;
using SpecGurka.Exceptions;
using SpecGurka.Interfaces;
using Gherkin.Ast;

namespace SpecGurka.WorkSystems.AzureDevOps;

public class AzureDevOpsWorkItemService : IWorkItemService
{
    private readonly UIHelper UI;
    private readonly AzureDevOpsConfig config;
    public AzureDevOpsWorkItemService(UIHelper UI, AzureDevOpsConfig config)
    {
        this.UI = UI;
        this.config = config;
    }

    public void VerifyWorkItemIsOfTypeFeature(WorkItem workItem)
    {
        var isFeature = IsWorkItemOfTypeFeature(workItem);

        if (!isFeature)
        {
            UI.PrintError("The work item is not a feature on Azure.");
            throw new NotFeatureException("The workitem is not a feature on Azure.");
        }

        UI.PrintOk("The work item is a feature on Azure.");
    }

    public bool IsWorkItemOfTypeFeature(WorkItem w)
    {
        if (w.ItemType.ToLower() == "feature")
            return true;

        return false;
    }

    public void VerifyGherkinTitleAndWorkItemTitleAreTheSame(GherkinDocument gherkinDoc, WorkItem serviceWorkItem)
    {
        var hasSameName = IsGherkinTitleAndWorkItemTitleTheSame(gherkinDoc, serviceWorkItem);

        if (!hasSameName)
        {
            UI.PrintError("The work item has different names in the gherkin file and on Azure.");
            throw new WrongFeatureConfigurationException("The title on Azure and the gherkin file title do not match.");
        }

        UI.PrintOk("The work item has the same name in both the gherkin file and on Azure.");
        return;
    }

    public bool IsGherkinTitleAndWorkItemTitleTheSame(GherkinDocument gherkinDoc, WorkItem serviceWorkItem)
    {
        if (gherkinDoc.Feature.Name.ToLower() == serviceWorkItem.Title.ToLower())
            return true;

        return false;
    }

    
}
