using SpecGurka.Config;
using SpecGurka.Exceptions;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using Gherkin.Ast;

namespace SpecGurka.WorkSystems.Monday;

public class MondayWorkItemService : IWorkItemService
{
    private readonly MondayConfig config;
    private readonly UIHelper UI;

    public MondayWorkItemService(MondayConfig config, UIHelper UI, GherkinFileService fileService)
    {
        this.config = config;
        this.UI = UI;
    }

    public void VerifyWorkItemIsOfTypeFeature(WorkItem workItem)
    {
        var isFeature = IsWorkItemOfTypeFeature(workItem);

        if (!isFeature)
        {
            UI.PrintError("The work item is not on the feature board on Monday.");
            throw new NotFeatureException("The workitem is not a feature on Monday.");
        }

        UI.PrintOk("The work item is on the feature board on Monday.");
    }

    public bool IsWorkItemOfTypeFeature(WorkItem w)
    {
        string boardName = config.FeatureBoardTitle;

        if (w.ItemType.ToLower() == boardName)
            return true;

        return false;
    }

    public void VerifyGherkinTitleAndWorkItemTitleAreTheSame(GherkinDocument gherkinDoc, WorkItem serviceWorkItem)
    {
        var hasSameName = IsGherkinTitleAndWorkItemTitleTheSame(gherkinDoc, serviceWorkItem);

        if (!hasSameName)
        {
            UI.PrintError("The work item has different names in the gherkin file and on Monday.");
            throw new WrongFeatureConfigurationException("The title on Monday and the gherkin file title do not match.");
        }

        UI.PrintOk("The work item has the same name in both the gherkin file and on Monday.");
        return;
    }

    public bool IsGherkinTitleAndWorkItemTitleTheSame(GherkinDocument gherkinWorkItem, WorkItem serviceWorkItem)
    {
        if (gherkinWorkItem.Feature.Name.ToLower() == serviceWorkItem.Title.ToLower())
            return true;

        return false;
    }
}
