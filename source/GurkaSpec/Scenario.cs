namespace SpecGurka.GurkaSpec;

public class Scenario
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Examples { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public bool IsOutline { get; set; } = false;

    public Status Status
    {
        get
        {
            if (Tags.Contains("@ignore"))
            {
                return Status.NotImplemented;
            }

            if (Steps.Any(step => step.Status == Status.Failed))
            {
                return Status.Failed;
            }

            if (Steps.Any(step => step.Status == Status.NotImplemented))
            {
                return Status.NotImplemented;
            }

            if (Steps.All(step => step.Status == Status.Passed))
            {
                return Status.Passed;
            }

            return Status.NotImplemented;
        }
        set { }
    }

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