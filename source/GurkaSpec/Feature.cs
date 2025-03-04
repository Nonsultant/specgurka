using System.Text;
using System.Text.RegularExpressions;
using TrxFileParser.Models;

namespace SpecGurka.GurkaSpec;

public class Feature
{
    public Guid Id = Guid.NewGuid();
    public required string Name { get; set; }


    public string FilePath { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new List<string>();

    public Status Status
    {
        get
        {
            if (Scenarios.All(scenario => scenario.Status == Status.Passed) &&
                (Background == null || Background.Status == Status.Passed) &&
                (Rules.Count == 0 || Rules.All(rule => rule.Status == Status.Passed)))
            {
                return Status.Passed;
            }

            if (Scenarios.Any(scenario => scenario.Status == Status.Failed) ||
                (Background != null && Background.Status == Status.Failed) ||
                (Rules.Count > 0 && Rules.Any(rule => rule.Status == Status.Failed)))
            {
                return Status.Failed;
            }

            return Status.NotImplemented;
        }
        set { }
    }

    public string? Description { get; set; }
    public Background? Background { get; set; }

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
            foreach (var rule in Rules)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(rule.TestDuration));
            }
            return _testDuration.ToString(@"hh\:mm\:ss\.fffffff");
        }
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Scenario> Scenarios { get; set; } = [];
    public List<Rule> Rules { get; set; } = [];

    public Scenario? GetScenario(UnitTestResult utr)
    {
        if (utr.Output == null || string.IsNullOrEmpty(utr.Output.StdOut))
        {
            return null;
        }

        string stdOut = utr.Output.StdOut;
        var firstLines = string.Join(" ", stdOut.Split('\n').Take(10));

        Scenario? matchedScenario = null;

        matchedScenario = TryMatchScenarioByName(utr.TestName, stdOut);
        if (matchedScenario != null)
        {
            return matchedScenario;
        }

        matchedScenario = TryOriginalMatching(utr.TestName, stdOut);
        if (matchedScenario != null)
        {
            return matchedScenario;
        }

        foreach (var rule in Rules)
        {
            var ruleScenario = rule.GetScenario(utr);
            if (ruleScenario is not null)
            {
                return ruleScenario;
            }
        }
        return null;
    }

    private Scenario? TryMatchScenarioByName(string testName, string testOutput)
    {
        string cleanTestName = testName
            .Replace("_", " ")
            .Replace(".", " ")
            .ToLower();

        foreach (var scenario in Scenarios)
        {
            string cleanScenarioName = scenario.Name
                .Replace("å", "a")
                .Replace("ä", "a")
                .Replace("ö", "o")
                .Replace("Å", "A")
                .Replace("Ä", "A")
                .Replace("Ö", "O")
                .ToLower();

            if (cleanTestName.Contains(cleanScenarioName) ||
                cleanScenarioName.Contains(cleanTestName))
            {
                if (scenario.Steps.Any(step => testOutput.Contains(step.Text.Split("<")[0])))
                {
                    return scenario;
                }
            }
        }

        return null;
    }

    private Scenario? TryOriginalMatching(string testName, string testOutput)
    {
        foreach (var scenario in Scenarios)
        {
            string normalizedTestName = testName
                .Replace("_", "")
                .ToLower();

            string normalizedScenarioName = scenario.Name
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
                .ToLower();

            bool nameMatches = normalizedTestName.StartsWith(normalizedScenarioName);

            if (nameMatches)
            {
                bool allStepsMatch = true;
                foreach (var step in scenario.Steps)
                {
                    string stepText = step.Text.Split("<")[0];
                    if (!testOutput.Contains(stepText))
                    {
                        allStepsMatch = false;
                        break;
                    }
                }

                if (allStepsMatch)
                {
                    return scenario;
                }
            }
        }

        return null;
    }

    public void ParseTestOutput(string output, Scenario sceUnderTest)
    {
        var outputSteps = StructureOutput(output);

        foreach (var outputStep in outputSteps)
        {

            Regex regex1 = new Regex(@"^(?<kind>\w+)\s(?<text>.+?)\s*->");
            var match1 = regex1.Match(outputStep);

            var kind = match1.Groups["kind"].Value;
            var text = match1.Groups["text"].Value;
            var steps = GetSteps(sceUnderTest, kind, text);

            if (steps == null || !steps.Any()) continue;

            Regex regex2 = new Regex(@"->\s*(?<status>\w+):\s(?<text>.+?)\s\((?<time>\d+[\.,]\d+)s\)$");
            var match2 = regex2.Match(outputStep);

            var status = match2.Groups["status"].Value;
            var methodText = match2.Groups["text"].Value;
            var time = match2.Groups["time"].Value.Replace(",", ".");

            foreach (var step in steps)
            {
                if (status.Contains("error"))
                {
                    step.Status = Status.Failed;
                    step.TestErrorMessage = methodText;
                }
                else
                {
                    step.Status = Status.Passed;
                    step.TestMethod = methodText;
                }
                step.TestDurationSeconds = time;
            }
        }
    }
    static List<string> StructureOutput(string output)
    {
        if (ContainsPattern(output))
        {
            output = GetLinesAfterTestContextMessages(output);
        }

        var cleanedOutput = output.Replace("\r", "");
        var lines = cleanedOutput.Split('\n');

        var allSteps = new List<string>();
        var currentStep = new StringBuilder();

        foreach (var line in lines)
        {
            currentStep.Append(line);

            if (!Regex.IsMatch(line, @"\(\d+[\.,]\d+s\)$")) continue;

            allSteps.Add(currentStep.ToString());
            currentStep.Clear();
        }

        return allSteps;
    }

    static bool ContainsPattern(string input)
    {
        var regex = new Regex(@"\r\s*\r\s*\r\s*TestContext Messages:\r");
        return regex.IsMatch(input);
    }

    static string GetLinesAfterTestContextMessages(string output)
    {
        var regex = new Regex(@"TestContext Messages:(.*)", RegexOptions.Singleline);
        var match = regex.Match(output);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    List<Step>? GetSteps(Scenario scenario, string kind, string text)
    {
        var steps = new List<Step>();

        var scenarioSteps = scenario.GetSteps(kind, text);
        if (scenarioSteps.Any())
        {
            steps.AddRange(scenarioSteps);
        }

        if (Background is not null)
        {
            var backgroundSteps = Background.GetSteps(kind, text);
            if (backgroundSteps.Any())
            {
                steps.AddRange(backgroundSteps);
            }
        }

        foreach (var rule in Rules)
        {
            if (rule.Background is not null)
            {
                var ruleBackgroundSteps = rule.Background.GetSteps(kind, text);
                if (ruleBackgroundSteps.Any())
                {
                    steps.AddRange(ruleBackgroundSteps);
                }
            }
        }

        return steps.Any() ? steps : null;
    }

}