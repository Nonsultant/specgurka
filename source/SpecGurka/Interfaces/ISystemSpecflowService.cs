using SpecGurka.Specflow;

namespace SpecGurka.Interfaces;

public interface ISystemSpecflowService
{
    Task UpdateSystemWithSpecflowResults(WorkItem serviceFeatureItem, SpecflowFeatureResult specflowResult);
}
