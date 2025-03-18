namespace SpecGurka.GurkaSpec;

public class Scenario
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Examples { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public bool IsOutline { get; set; } = false;
    public bool IsOutlineChild { get; set; } = false;

    public Status Status { get; set; }

    private TimeSpan _testDuration;
    public string TestDuration
    {
        get => _testDuration.ToString(@"hh\:mm\:ss\.fffffff");
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Step> Steps { get; set; } = [];

    public List<Step> GetSteps(string kind, string text)
    {
        var steps = Steps.Where(s => text.Contains(s.Text) && s.Kind == kind).ToList();
        return steps;
    }
}