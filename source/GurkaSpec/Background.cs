using System.Xml.Serialization;

namespace SpecGurka.GurkaSpec;

public class Background
{
    public string? Name { get; set; }

    public bool TestsPassed
    {
        get
        {
            bool testsPassed = true;
            foreach (var step in Steps)
            {
                if (!step.TestPassed)
                {
                    testsPassed = false;
                    break;
                }
            }

            return testsPassed;
        }
        set {}
    }

    [XmlIgnore]
    public string ErrorMessage { get; set; }

    private TimeSpan _testDuration;
    public string TestDuration
    {
        get => _testDuration.ToString();
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Step> Steps { get; set; } = [];
}