namespace SpecGurka.GurkaSpec;

public class Rule
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Background? Background { get; set; }

    public Status Status
    {
        get
        {
            if (Scenarios.All(scenario => scenario.Status == Status.Passed) &&
                (Background == null || Background.Status == Status.Passed))
            {
                return Status.Passed;
            }

            if (Scenarios.Any(scenario => scenario.Status == Status.Failed) ||
                (Background != null && Background.Status == Status.Failed))
            {
                return Status.Failed;
            }

            return Status.NotImplemented;
        }
        set{}
    }

    private TimeSpan _testDuration;
    public string TestDuration
    {
        get
        {
            _testDuration = TimeSpan.Zero;
            foreach (var scenario in Scenarios)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(scenario.TestDuration));
            }

            return _testDuration.ToString();
        }
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Scenario> Scenarios { get; set; } = [];

    public Scenario? GetScenario(string name)
    {
        var scenario = Scenarios.FirstOrDefault(s => s.Name
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o")
            .Replace("Å", "A")
            .Replace("Ä", "A")
            .Replace("Ö", "O")
            .Replace(" ", "")
            .Trim()
            .ToLower() == name.ToLower());
        return scenario;
    }
}