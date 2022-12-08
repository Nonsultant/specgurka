namespace SpecGurka.Specflow;

public class SpecflowFeatureResult
{
    public List<Scenario> Scenarios = new();
    public string Title { get; set; }

    public bool AllScenariosAreOK()
    {
        bool allOK = true;

        foreach (var scenario in Scenarios)
        {
            if (scenario.Status != "OK")
                allOK = false;
        }

        return allOK;
    }

}
