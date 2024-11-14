using Gherkin.Ast;

namespace SpecGurka.Interfaces;

public interface IWorkItemService
{
    public bool IsWorkItemOfTypeFeature(WorkItem w);
    public bool IsGherkinTitleAndWorkItemTitleTheSame(GherkinDocument gherkinDoc, WorkItem serviceWorkItem);
    public void VerifyWorkItemIsOfTypeFeature(WorkItem workitem);
    public void VerifyGherkinTitleAndWorkItemTitleAreTheSame(GherkinDocument gherkinDoc, WorkItem serviceWorkItem);

}
