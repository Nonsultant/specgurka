﻿namespace SpecGurka.GurkaSpec;

public class Feature
{
    public required string Name { get; set; }
    public bool TestsPassed { get; set; }
    public string? Description { get; set; }
    public Background? Background { get; set; }

    private TimeSpan _testDuration;
    public string TestDuration {
        get {
            _testDuration = TimeSpan.Zero;
            foreach (var scenario in Scenarios)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(scenario.TestDuration));
            }

            if (Background is not null)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(Background.TestDuration));
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

    public Background? GetBackground(string name)
    {
        if (Background is not null && Background.Name == name)
        {
            return Background;
        }

        foreach (var rule in Rules)
        {
            if (rule.Background is not null && rule.Background.Name == name)
            {
                return rule.Background;
            }
        }

    public Rule? GetRule(string name)
        return null;
    }
    {
        var rule = Rules.FirstOrDefault(s => s.Name == name);
        return rule;
    }

    public void ParseTestOutput(string output)
    {
        var lines = output.Split('\n');

        var allSteps = new List<string>();
        var currentStep = new StringBuilder();

        foreach (var line in lines)
        {
            currentStep.Append(line);

            if (!Regex.IsMatch(line, @"\(\d+\.\d+s\)$")) continue;

            allSteps.Add(currentStep.ToString());
            currentStep.Clear();
        }

        foreach (var outputStep in allSteps)
        {
            Regex regex1 = new Regex(@"^(?<kind>\w+)\s(?<text>.+?)\s*->");
            var match1 = regex1.Match(outputStep);

            if (!match1.Success)
                continue;

            var kind = match1.Groups["kind"].Value;
            var text = match1.Groups["text"].Value;
            var steps = GetSteps(kind, text);

            Regex regex2 = new Regex(@"->\s*(?<status>\w+):\s(?<text>.+?)\s\((?<time>\d+\.\d+)s\)$");
            var match2 = regex2.Match(outputStep);

            if (!match2.Success)
                continue;

            var status = match2.Groups["status"].Value;
            var methodText = match2.Groups["text"].Value;
            var time = match2.Groups["time"].Value.Replace(",", ".");

            foreach (var step in steps)
            {
                if (status.Contains("error"))
                {
                    step.TestPassed = false;
                    step.TestErrorMessage = methodText;
                }
                else
                {
                    step.TestPassed = true;
                    step.TestMethod = methodText;
                }
                step.TestDurationSeconds = time;
            }
        }
    }

}