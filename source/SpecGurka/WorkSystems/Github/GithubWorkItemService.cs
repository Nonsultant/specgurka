using SpecGurka.Config;
using SpecGurka.Exceptions;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using Gherkin.Ast;

namespace SpecGurka.WorkSystems.Github;

public class GithubWorkItemService : IWorkItemService
{
    private readonly GithubConfig config;
    private readonly UIHelper UI;

    public GithubWorkItemService(GithubConfig config, UIHelper UI)
    {
        this.config = config;
        this.UI = UI;
    }

    public void VerifyWorkItemIsOfTypeFeature(WorkItem workItem)
    {
        var isFeature = IsWorkItemOfTypeFeature(workItem);

        if (!isFeature)
        {
            UI.PrintError("The work item is not a feature on Github.");
            throw new NotFeatureException("The work item is not a feature on Github.");
        }

        UI.PrintOk("The work item is a feature on Github.");
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
            UI.PrintError("The work item has different names in the gherkin file and on Github.");
            throw new WrongFeatureConfigurationException("The title on Github and the gherkin file title do not match.");
        }

        UI.PrintOk("The work item has the same name in both the gherkin file and on Github.");
        return;
    }

    public bool IsGherkinTitleAndWorkItemTitleTheSame(GherkinDocument gherkinDoc, WorkItem serviceWorkItem)
    {
        if (gherkinDoc.Feature.Name.ToLower() == serviceWorkItem.Title.ToLower())
            return true;

        return false;
    }
}
