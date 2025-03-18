using SpecGurka.GurkaSpec;
using SpecGurka.GenGurka.Helpers;
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
                string cleanKey = ScenarioOutlineExtensions.ExtractScenarioNameFromTestName(utr.TestName);

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
            ScenarioOutlineExtensions.ProcessScenarios(feature.Scenarios, testResults);

            foreach (var rule in feature.Rules)
            {
                ScenarioOutlineExtensions.ProcessScenarios(rule.Scenarios, testResults);
            }
        }

        // This needs to be outside the feature loop!
        StatusPropagationHelper.PropagateStatusesToParents(gurkaProject);

        // Force recalculations
        foreach (var feature in gurkaProject.Features)
        {
            foreach (var rule in feature.Rules)
            {
                string ruleDuration = rule.TestDuration;
            }

            string featureDuration = feature.TestDuration;
        }

        string productDuration = gurkaProject.TestDuration;
    }

    public static UnitTestResult? FindMatchingTestResult(Scenario scenario, Dictionary<string, List<UnitTestResult>> testResults)
    {
        string scenarioName = scenario.Name;
        var exampleParams = new Dictionary<string, string>();

        //try exact match by normalized name
        string cleanName = NormalizeText(scenarioName);
        if (testResults.TryGetValue(cleanName, out var directMatches) && directMatches.Count > 0)
        {
            return directMatches[0];
        }

        // For scenario outline, try to match by example parameters
        if (!string.IsNullOrEmpty(scenario.Examples) && scenario.Examples.StartsWith("Example:"))
        {
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
        }

        foreach (var entry in testResults)
        {
            // scenario name AND parameter values?
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
                    return entry.Value[0];
                }
            }
        }

        // output containing parameter values?
        foreach (var entry in testResults)
        {
            foreach (var result in entry.Value)
            {
                if (result.Output?.StdOut == null) continue;

                bool allParamsFound = true;
                foreach (var param in exampleParams)
                {
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

        // scenario name embedded in test name?
        foreach (var entry in testResults)
        {
            if ((entry.Key.Contains(scenarioName, StringComparison.OrdinalIgnoreCase) ||
                NormalizeText(entry.Key).Contains(cleanName, StringComparison.OrdinalIgnoreCase)))
            {
                return entry.Value[0];
            }
        }

        // exact step text in output?
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
            // multi-word match?
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

                if (allWordsFound && matchedWords > 1)
                {
                    return entry.Value[0];
                }
            }

            // individual significant word match?
            var significantWords = scenarioWords.Where(w => w.Length > 4).ToArray();
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
}