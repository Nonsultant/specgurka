﻿namespace SpecGurka.GurkaSpec;

public class Rule
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Background? Background { get; set; }

    public Status Status
    {
        get
        {
            bool testsPassed = true;
            foreach (var scenario in Scenarios)
            {
                if (scenario.Status == Status.Failed)
                {
                    testsPassed = false;
                    break;
                }
            }

            if (testsPassed)
            {
                return Status.Passed;
            }

            return Status.Failed;
        }
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