using SpecGurka.GurkaSpec;
using TrxFileParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpecGurka.GenGurka.Extensions;

internal static class TestRunExtensions
{
    internal static void MatchWithGurkaFeatures(this TestRun testRun, Product gurkaProject)
    {
        // results by name
        var testResults = new Dictionary<string, List<UnitTestResult>>(StringComparer.OrdinalIgnoreCase);

        foreach (var utr in testRun.Results.UnitTestResults)
        {
            if (utr.Outcome != "NotExecuted" && !string.IsNullOrEmpty(utr.Duration))
            {
                string cleanKey = ExtractScenarioNameFromTestName(utr.TestName);

                if (!testResults.ContainsKey(cleanKey))
                {
                    testResults[cleanKey] = new List<UnitTestResult>();
                }

                testResults[cleanKey].Add(utr);

                if (!testResults.ContainsKey(utr.TestName))
                {
                    testResults[utr.TestName] = new List<UnitTestResult>();
                }
                testResults[utr.TestName].Add(utr);
            }
        }

        foreach (var feature in gurkaProject.Features)
        {
            ProcessScenarios(feature.Scenarios, testResults);

            foreach (var rule in feature.Rules)
            {
                ProcessScenarios(rule.Scenarios, testResults);
            }

            // Force recalculations
            foreach (var rule in feature.Rules)
            {
                string ruleDuration = rule.TestDuration;
            }

            string featureDuration = feature.TestDuration;
        }

        string productDuration = gurkaProject.TestDuration;
    }

    private static void ProcessScenarios(List<Scenario> scenarios, Dictionary<string, List<UnitTestResult>> testResults)
    {
        var processedTestIds = new HashSet<string>();

        //regular scenarios first, then outlines
        var regularScenarios = scenarios.Where(s => !s.IsOutline && !s.IsOutlineChild).ToList();
        var outlineTemplates = scenarios.Where(s => s.IsOutline).ToList();
        var outlineInstances = scenarios.Where(s => s.IsOutlineChild).ToList();

        foreach (var scenario in regularScenarios)
        {
            ProcessScenario(scenario, testResults, processedTestIds);
        }

        foreach (var scenario in outlineTemplates)
        {
            ProcessScenario(scenario, testResults, processedTestIds);
        }

        foreach (var scenario in outlineInstances)
        {
            ProcessScenario(scenario, testResults, processedTestIds);
        }
    }

    private static void ProcessScenario(Scenario scenario, Dictionary<string, List<UnitTestResult>> testResults, HashSet<string> processedTestIds)
    {
        // Skip if scenario already has a non-zero duration
        if (!IsZeroDuration(scenario.TestDuration))
        {
            return;
        }

        var matchedResult = FindMatchingTestResult(scenario, testResults);

        if (matchedResult != null)
        {
            if (processedTestIds.Contains(matchedResult.TestId))
            {
                return;
            }

            ApplyTestStatusToScenario(scenario, matchedResult);

            if (TimeSpan.TryParse(matchedResult.Duration, out TimeSpan duration))
            {
                string formattedDuration = duration.ToString(@"hh\:mm\:ss\.fffffff");
                scenario.TestDuration = formattedDuration;

                processedTestIds.Add(matchedResult.TestId);
            }
        }

        else
        {
            if (scenario.Status == Status.Passed)
            {
                scenario.TestDuration = TimeSpan.Zero.ToString(@"hh\:mm\:ss\.fffffff");
            }
        }
    }

    private static UnitTestResult? FindMatchingTestResult(Scenario scenario, Dictionary<string, List<UnitTestResult>> testResults)
    {
        string scenarioName = scenario.Name;

        // First, try exact match by normalized name
        string cleanName = NormalizeText(scenarioName);
        if (testResults.TryGetValue(cleanName, out var directMatches) && directMatches.Count > 0)
        {
            Console.WriteLine($"DEBUG: Found direct match for '{scenarioName}' by normalized name");
            return directMatches[0];
        }

        // For scenario outline, try to match by example parameters
        if (!string.IsNullOrEmpty(scenario.Examples) && scenario.Examples.StartsWith("Example:"))
        {
            var exampleParams = new Dictionary<string, string>();
            string exampleContent = scenario.Examples.Substring("Example: ".Length);

            var pairs = exampleContent.Split(',');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    exampleParams[key] = value;
                }
            }

            //try to match by scenario name + parameters
            foreach (var entry in testResults)
            {
                // Check if contains both the scenario name AND parameter values
                bool containsScenarioName = entry.Key.Contains(scenarioName, StringComparison.OrdinalIgnoreCase) ||
                                           NormalizeText(entry.Key).Contains(cleanName, StringComparison.OrdinalIgnoreCase);

                if (containsScenarioName)
                {
                    bool allParamsInName = true;
                    foreach (var param in exampleParams)
                    {
                        if (!entry.Key.Contains(param.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            allParamsInName = false;
                            break;
                        }
                    }

                    if (allParamsInName && exampleParams.Count > 0)
                    {
                        Console.WriteLine($"DEBUG: Found match for outline example with parameters in test name: {entry.Key}");
                        return entry.Value[0];
                    }
                }
            }

            // Then, try to match by output content containing parameter values
            foreach (var entry in testResults)
            {
                foreach (var result in entry.Value)
                {
                    if (result.Output?.StdOut == null) continue;

                    bool allParamsFound = true;
                    foreach (var param in exampleParams)
                    {
                        // Check if the parameter value is in the test output
                        if (!result.Output.StdOut.Contains(param.Value))
                        {
                            allParamsFound = false;
                            break;
                        }
                    }

                    if (allParamsFound && exampleParams.Count > 0)
                    {
                        return result;
                    }
                }
            }
        }

        // Match by scenario name embedded in test name
        foreach (var entry in testResults)
        {
            if (entry.Key.Contains(scenarioName, StringComparison.OrdinalIgnoreCase) ||
                NormalizeText(entry.Key).Contains(cleanName, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value[0];
            }
        }

        // Match by exact step text in output
        foreach (var entry in testResults)
        {
            foreach (var result in entry.Value)
            {
                if (result.Output?.StdOut != null && scenario.Steps.Count > 0)
                {
                    bool allStepsFound = true;
                    int matchedSteps = 0;
                    foreach (var step in scenario.Steps)
                    {
                        string stepText = step.Text.Split('<')[0].Trim();
                        if (!result.Output.StdOut.Contains(stepText))
                        {
                            allStepsFound = false;
                            break;
                        }
                        matchedSteps++;
                    }

                    if (allStepsFound && matchedSteps > 0)
                    {
                        return result;
                    }
                }
            }
        }

        // Exact words match
        string[] scenarioWords = scenarioName.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (scenarioWords.Length > 0)
        {
            // Try multi-word match first
            foreach (var entry in testResults)
            {
                bool allWordsFound = true;
                int matchedWords = 0;
                foreach (var word in scenarioWords)
                {
                    if (word.Length < 3) continue;

                    if (!entry.Key.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        allWordsFound = false;
                        break;
                    }
                    matchedWords++;
                }

                if (allWordsFound && matchedWords > 1) // Require at least 2 words to match
                {
                    return entry.Value[0];
                }
            }

            // Finally, try individual significant word match
            var significantWords = scenarioWords.Where(w => w.Length > 4).ToArray(); // Use only longer words
            if (significantWords.Length > 0)
            {
                foreach (var entry in testResults)
                {
                    foreach (var word in significantWords)
                    {
                        if (entry.Key.Contains(word, StringComparison.OrdinalIgnoreCase))
                        {
                            return entry.Value[0];
                        }
                    }
                }
            }
        }
        
        return null;
    }
    private static void ApplyTestStatusToScenario(Scenario scenario, UnitTestResult result)
    {
        // Set the scenario status based on the <outcome> property
        switch (result.Outcome)
        {
            case "Passed":
                scenario.Status = Status.Passed;
                foreach (var step in scenario.Steps)
                {
                    step.Status = Status.Passed;
                }
                break;
            case "Failed":
                scenario.Status = Status.Failed;

                if (result.Output?.StdOut != null)
                {
                    ApplyStepStatuses(scenario, result.Output.StdOut);
                }
                else
                {
                    // If we can't identify specific failed steps, mark all as failed
                    // Because if that data is missing something is wrong anyway...
                    foreach (var step in scenario.Steps)
                    {
                        step.Status = Status.Failed;
                    }
                }
                break;
            case "NotExecuted":
                scenario.Status = Status.NotImplemented;
                foreach (var step in scenario.Steps)
                {
                    step.Status = Status.NotImplemented;
                }
                break;
            default:

                break;
        }
    }


    //This should kind of simulate the behaviour of how Reqnroll handles step statuses
    //If a step fails, all subsequent steps are marked as skipped. In our case we mark them as not implemented
    private static void ApplyStepStatuses(Scenario scenario, string output)
    {
        // Parse the test output to identify step results
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int currentStepIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            // Look for step text in the output
            for (int stepIdx = 0; stepIdx < scenario.Steps.Count; stepIdx++)
            {
                var step = scenario.Steps[stepIdx];
                if (line.Contains(step.Text))
                {
                    currentStepIndex = stepIdx;
                    break;
                }
            }

            // Look for step result indicators
            if (currentStepIndex >= 0 && currentStepIndex < scenario.Steps.Count)
            {
                var currentStep = scenario.Steps[currentStepIndex];

                // Check next line for step status
                if (i + 1 < lines.Length)
                {
                    string nextLine = lines[i + 1];
                    if (nextLine.Contains("-> done:"))
                    {
                        currentStep.Status = Status.Passed;
                    }
                    else if (nextLine.Contains("-> error:"))
                    {
                        currentStep.Status = Status.Failed;

                        // Mark all subsequent steps as skipped
                        for (int j = currentStepIndex + 1; j < scenario.Steps.Count; j++)
                        {
                            scenario.Steps[j].Status = Status.NotImplemented;
                        }

                        break;
                    }
                    else if (nextLine.Contains("-> skipped"))
                    {
                        currentStep.Status = Status.NotImplemented;
                    }
                }
            }
        }
    }


    private static string ExtractScenarioNameFromTestName(string testName)
    {
        string name = testName;

        if (name.Contains("."))
        {
            name = name.Split('.').Last();
        }

        name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

        return name.ToLowerInvariant();
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace(" ", "")
        .Replace("_", "")
        .Replace("-", "")
        .Replace("å", "a")
        .Replace("ä", "a")
        .Replace("ö", "o")
        .Replace("Å", "A")
        .Replace("Ä", "A")
        .Replace("Ö", "O")
        .ToLowerInvariant();
    }

    private static bool IsZeroDuration(string duration)
    {
        if (string.IsNullOrEmpty(duration))
        {
            return true;
        }

        // Check common zero duration string formats
        if (duration == "00:00:00" ||
            duration == "00:00:00.0000000" ||
            duration == "00:00:00.000000" ||
            duration == "00:00:00.00000" ||
            duration == "00:00:00.0000" ||
            duration == "00:00:00.000" ||
            duration == "00:00:00.00" ||
            duration == "00:00:00.0" ||
            duration == "0" ||
            duration == "0.0")
        {
            return true;
        }

        // Try parsing as TimeSpan
        if (TimeSpan.TryParse(duration, out TimeSpan ts))
        {
            return ts.TotalMilliseconds == 0;
        }

        // If we can't parse it, assume it's not zero
        return false;
    }
}