using SpecGurka.Specflow;

namespace SpecGurka.Interfaces;

public interface ISystemClient
{
    Task<WorkItem> GetWorkItemFromSystem(string workItemId);
}
