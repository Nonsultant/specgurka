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
        // Try to match each scenario with test results
        foreach (var scenario in scenarios)
        {
            // Skip if scenario already has a non-zero duration
            if (!IsZeroDuration(scenario.TestDuration))
            {
                Console.WriteLine($"Scenario already has non-zero duration: {scenario.TestDuration}");
                continue;
            }

            UnitTestResult matchedResult = FindMatchingTestResult(scenario, testResults);

            if (matchedResult != null)
            {
                if (TimeSpan.TryParse(matchedResult.Duration, out TimeSpan duration))
                {
                    scenario.TestDuration = duration.ToString(@"hh\:mm\:ss\.fffffff");
                    // pop from dictionary
                    foreach (var key in testResults.Keys.ToList())
                    {
                        testResults[key].RemoveAll(r => r.TestId == matchedResult.TestId);
                        // Remove empty lists
                        if (!testResults[key].Any())
                        {
                            testResults.Remove(key);
                        }
                    }
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
    }

    private static UnitTestResult? FindMatchingTestResult(Scenario scenario, Dictionary<string, List<UnitTestResult>> testResults)
    {
        string scenarioName = scenario.Name;

        // handle scenario outline, identified by if they have an examples property or not
        if (!string.IsNullOrEmpty(scenario.Examples) && scenario.Examples.StartsWith("Example:"))
        {
            // extract parameter values from the <Examples> property
            var exampleParams = new Dictionary<string, string>();


            string exampleContent = scenario.Examples.Substring("Example: ".Length);

            // extract key-value pairs
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

            // Now check test results that contain these specific parameter values
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
                        // Apply the test status to the scenario
                        ApplyTestStatusToScenario(scenario, result);
                        return result;
                    }
                }
            }
        }

        // match by name
        string cleanName = NormalizeText(scenarioName);
        if (testResults.TryGetValue(cleanName, out var directMatches) && directMatches.Count > 0)
        {
            ApplyTestStatusToScenario(scenario, directMatches[0]);
            return directMatches[0];
        }

        // match by scenario name embedded in test name
        foreach (var entry in testResults)
        {
            if (entry.Key.Contains(scenarioName, StringComparison.OrdinalIgnoreCase) ||
                NormalizeText(entry.Key).Contains(cleanName, StringComparison.OrdinalIgnoreCase))
            {
                ApplyTestStatusToScenario(scenario, entry.Value[0]);
                return entry.Value[0];
            }

            foreach (var result in entry.Value)
            {
                if (result.Output?.StdOut != null)
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
                        ApplyTestStatusToScenario(scenario, result);
                        return result;
                    }
                }
            }
        }

        // exact words match
        string[] scenarioWords = scenarioName.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (scenarioWords.Length > 0)
        {
            foreach (var entry in testResults)
            {
                bool allWordsFound = true;
                foreach (var word in scenarioWords)
                {
                    if (word.Length < 3) continue;

                    if (!entry.Key.Contains(word, StringComparison.OrdinalIgnoreCase))
                    {
                        allWordsFound = false;
                        break;
                    }
                }

                if (allWordsFound)
                {
                    ApplyTestStatusToScenario(scenario, entry.Value[0]);
                    return entry.Value[0];
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

        return duration == "00:00:00" ||
        duration == "00:00:00.0000000" ||
        duration == "0" ||
        (TimeSpan.TryParse(duration, out TimeSpan ts) && ts == TimeSpan.Zero);
    }
}