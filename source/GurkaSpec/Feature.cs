namespace SpecGurka.GurkaSpec;

public class Feature
{
    public required string Name { get; set; }
    public bool TestsPassed { get; set; }
    public string? Description { get; set; }

    private TimeSpan _testDuration;
    public string TestDuration {
        get {
            _testDuration = TimeSpan.Zero;
            foreach (var scenario in Scenarios)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(scenario.TestDuration));
            }

            foreach (var rule in Rules)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(rule.TestDuration));
            }

            return _testDuration.ToString();
        }
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Scenario> Scenarios { get; set; } = [];
    public Scenario? GetScenario(string name)
    {
        Scenario? scenario = Scenarios.FirstOrDefault(s => s.Name == name);
        if(scenario == null)
        {
            foreach (var rule in Rules)
            {
                var ruleScenario = rule.GetScenario(name);
                if (ruleScenario != null)
                {
                    return ruleScenario;
                }
            }
        }
        return scenario;
    }

    public List<Rule> Rules { get; set; } = [];

    public Rule? GetRule(string name)
    {
        var rule = Rules.FirstOrDefault(s => s.Name == name);
        return rule;
    }
}