using Gherkin.Ast;

namespace SpecGurka.Interfaces;

public interface ISystemFileLinkService
{
    Task UpdateSystemFileLink(string fileLink, Feature feature);
    void VerifyGherkinFileLinkOnSystem(string gherkinFilePath, WorkItem serviceFeatureItem);
    string CleanSystemFileLink(string gherkinFilePath, string fileLink);
}
