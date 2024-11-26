using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SpecGurka.GurkaSpec;

public class Scenario
{
    public required string Name { get; set; }
    public bool TestPassed { get; set; }


    private TimeSpan _testDuration;
    public string TestDuration
    {
        get => _testDuration.ToString();
        set => _testDuration = TimeSpan.Parse(value);
    }

    [XmlIgnore]
    public string TestOutput { get; set; }

    [XmlIgnore]
    public string ErrorMessage { get; set; }

    public List<Step> Steps { get; set; } = [];

    public Step? GetStep(string kind, string text)
    {
        var step = Steps.FirstOrDefault(f => f.Text == text && f.Kind == kind);
        return step;
    }

    public void ParseTestError(string errorOutput)
    {
        //throw new NotImplementedException();
    }
}