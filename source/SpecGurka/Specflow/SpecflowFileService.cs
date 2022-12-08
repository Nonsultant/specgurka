using SpecGurka.Exceptions;

namespace SpecGurka.Specflow;

public class SpecflowFileService
{
    private readonly UIHelper UI;

    public SpecflowFileService(UIHelper ui)
    {
        this.UI = ui;
    }

    public List<SpecflowFeatureResult> ExtractSpecflowFeatureResultsFromExecutionResults(dynamic specflowExecutionResults)
    {
        List<SpecflowFeatureResult> specflowResults = new();

        foreach (var result in specflowExecutionResults)
        {
            SpecflowFeatureResult specflowResult = new();
            specflowResult.Title = result["FeatureTitle"];

            bool featureExists = CheckIfFeatureExists(specflowResults, specflowResult);

            if (!featureExists)
            {
                specflowResult.Scenarios = ExtractScenariosFromSpecflowResults(specflowExecutionResults, specflowResult.Title);
                specflowResults.Add(specflowResult);
            }
        }

        return specflowResults;
    }

    private List<Scenario> ExtractScenariosFromSpecflowResults(dynamic specflowExecutionResults, string featureTitle)
    {
        List<Scenario> scenarios = new();

        foreach (var result in specflowExecutionResults)
        {
            if (result["FeatureTitle"] == featureTitle)
            {
                Scenario scenario = new Scenario();
                scenario.Title = result["ScenarioTitle"];
                scenario.Status = result["Status"];

                scenarios.Add(scenario);
            }
        }

        return scenarios;
    }

    private bool CheckIfFeatureExists(List<SpecflowFeatureResult> specflowResults, SpecflowFeatureResult specflowResult)
    {
        bool featureExists = false;

        foreach (var f in specflowResults)
        {
            if (f.Title == specflowResult.Title)
                featureExists = true;
        }

        return featureExists;
    }

    private string GetIdFromServiceFeature(string specflowTitle, List<Feature> features)
    {
        foreach (var feature in features)
        {
            if (feature.ServiceFeatureItem == null || feature.ServiceFeatureItem.Title == null)
            {
                UI.PrintError($"{specflowTitle} either does not exist on the service or could not be found, so cannot be updated.");
                throw new NotFoundException("The feature could not be found on the service.");
            }

            if (specflowTitle == feature.ServiceFeatureItem.Title)
                return feature.ServiceFeatureItem.Id;
        }

        UI.PrintError($"{specflowTitle} either does not exist on the service or could not be found, so cannot be updated.");
        throw new NotFoundException("The feature does not have a ServiceFeatureItem.");
    }
}
