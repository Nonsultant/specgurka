﻿using TrxFileParser.Models;

namespace SpecGurka.GurkaSpec;

public class Rule
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Background? Background { get; set; }
    public List<string> Tags { get; set; } = new List<string>();

    public Status Status { get; set; }

    private TimeSpan _testDuration;
    public string TestDuration
    {
        get
        {
            _testDuration = TimeSpan.Zero;
            foreach (var scenario in Scenarios)
            {
                if (TimeSpan.TryParse(scenario.TestDuration, out var duration))
                {
                    _testDuration = _testDuration.Add(duration);
                }
            }
            return _testDuration.ToString(@"hh\:mm\:ss\.fffffff");
        }
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Scenario> Scenarios { get; set; } = [];

    public Scenario? GetScenario(UnitTestResult utr)
    {
        var scenario = Scenarios.FirstOrDefault(s => utr.TestName
            .Replace("_", "")
            .ToLower()
            .StartsWith(s.Name
                .Replace("å", "a")
                .Replace("ä", "a")
                .Replace("ö", "o")
                .Replace("Å", "A")
                .Replace("Ä", "A")
                .Replace("Ö", "O")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Trim()
                .ToLower()) && s.Steps.All(step => utr.Output.StdOut.Contains(step.Text.Split("<")[0])));

        return scenario;
    }
}