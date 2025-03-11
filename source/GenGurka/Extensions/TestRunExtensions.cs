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

        // match by name
        string cleanName = NormalizeText(scenarioName);
        if (testResults.TryGetValue(cleanName, out var directMatches) && directMatches.Count > 0)
        {
            return directMatches[0];
        }

        // match by scenario name embedded in test name
        foreach (var entry in testResults)
        {
            if (entry.Key.Contains(scenarioName, StringComparison.OrdinalIgnoreCase) ||
                NormalizeText(entry.Key).Contains(cleanName, StringComparison.OrdinalIgnoreCase))
            {
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
                    return entry.Value[0];
                }
            }
        }

        return null;
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