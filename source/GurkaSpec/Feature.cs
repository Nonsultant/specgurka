using System.Text;
using System.Text.RegularExpressions;

namespace SpecGurka.GurkaSpec;

public class Feature
{
    public Guid Id = Guid.NewGuid();
    public required string Name { get; set; }

    public bool TestsPassed
    {
        get => Scenarios.All(scenario => scenario.TestsPassed) &&
                                   (Background?.TestsPassed ?? true) &&
                                   Rules.All(rule => rule.TestsPassed);
        set{}
    }
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

            foreach (var rule in Rules)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(rule.TestDuration));
            }

            return _testDuration.ToString();
        }
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Scenario> Scenarios { get; set; } = [];
    public List<Rule> Rules { get; set; } = [];

    public Scenario? GetScenario(string name)
    {
        Scenario? scenario = Scenarios.FirstOrDefault(s => s.Name == name);
        if(scenario is null)
        {
            foreach (var rule in Rules)
            {
                var ruleScenario = rule.GetScenario(name);
                if (ruleScenario is not null)
                {
                    return ruleScenario;
                }
            }
        }
        return scenario;
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

            var kind = match1.Groups["kind"].Value;
            var text = match1.Groups["text"].Value;
            var steps = GetSteps(kind, text);


            Regex regex2 = new Regex(@"->\s*(?<status>\w+):\s(?<text>.+?)\s\((?<time>\d+\.\d+)s\)$");
            var match2 = regex2.Match(outputStep);

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

    private List<Step> GetSteps(string kind, string text)
    {
        var steps = new List<Step>();

        foreach (var scenario in Scenarios)
        {
            var scenarioSteps = scenario.GetSteps(kind, text);
            if (scenarioSteps.Any())
            {
                steps.AddRange(scenarioSteps);
            }
        }

        foreach (var rule in Rules)
        {
            if (rule.Background is not null)
            {
                var backgroundSteps = rule.Background.GetSteps(kind, text);
                if (backgroundSteps.Any())
                {
                    steps.AddRange(backgroundSteps);
                }
            }

            foreach (var scenario in rule.Scenarios)
            {
                var ruleScenarioSteps = scenario.GetSteps(kind, text);
                if (ruleScenarioSteps.Any())
                {
                    steps.AddRange(ruleScenarioSteps);
                }
            }
        }

        if (Background is not null)
        {
            var backgroundSteps = Background.GetSteps(kind, text);
            if (backgroundSteps.Any())
            {
                steps.AddRange(backgroundSteps);
            }
        }

        return steps.Any() ? steps : null;
    }

}