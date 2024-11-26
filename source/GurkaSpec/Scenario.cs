using System.Xml.Serialization;

namespace SpecGurka.GurkaSpec;

public class Scenario
{
    public required string Name { get; set; }

    public bool TestsPassed
    {
        get
        {
            bool testPassed = true;
            foreach (var step in Steps)
            {
                if (!step.TestPassed)
                {
                    testPassed = false;
                    break;
                }
            }

            return testPassed;
        }
        set {}
    }

    private TimeSpan _testDuration;
    public string TestDuration
    {
        get => _testDuration.ToString();
        set => _testDuration = TimeSpan.Parse(value);
    }

    [XmlIgnore]
    public string ErrorMessage { get; set; }

    public List<Step> Steps { get; set; } = [];

    public List<Step> GetSteps(string kind, string text)
    {
        var steps = Steps.Where(s => text.Contains(s.Text) && s.Kind == kind).ToList();
        return steps;
    }
}