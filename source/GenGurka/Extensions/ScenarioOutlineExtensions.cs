using SpecGurka.GurkaSpec;
using TrxFileParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpecGurka.GenGurka.Helpers;
using SpecGurka.GenGurka.Extensions;
namespace SpecGurka.GenGurka.Extensions;

internal static class ScenarioOutlineExtensions
{

    public static void ProcessScenarios(List<Scenario> scenarios, Dictionary<string, List<UnitTestResult>> testResults)
    {
        var processedTestIds = new HashSet<string>();

        // Separate scenarios by type
        var regularScenarios = scenarios.Where(s => !s.IsOutline && !s.IsOutlineChild).ToList();
        var outlineTemplates = scenarios.Where(s => s.IsOutline).ToList();
        var outlineInstances = scenarios.Where(s => s.IsOutlineChild).ToList();

        //regular scenario
        foreach (var scenario in regularScenarios)
        {
            ProcessScenario(scenario, testResults, processedTestIds);
        }

        //outline children
        foreach (var scenario in outlineInstances)
        {
            ProcessScenario(scenario, testResults, processedTestIds);
        }

        //outline templates based on their children
        foreach (var template in outlineTemplates)
        {
            var children = outlineInstances.Where(s => s.Name.StartsWith(template.Name)).ToList();

            if (children.Any())
            {
                if (children.Any(c => c.Status == Status.Failed))
                {
                    //any child failed, template is failed
                    template.Status = Status.Failed;
                }
                else if (children.All(c => c.Status == Status.NotImplemented))
                {
                    template.Status = Status.NotImplemented;
                }
                else if (children.Any(c => c.Status == Status.Passed))
                {
                    template.Status = Status.Passed;
                }

                // Calculate aggregate duration for template
                TimeSpan totalDuration = TimeSpan.Zero;
                foreach (var child in children)
                {
                    if (TimeSpan.TryParse(child.TestDuration, out var duration))
                    {
                        totalDuration = totalDuration.Add(duration);
                    }
                }
                template.TestDuration = totalDuration.ToString(@"hh\:mm\:ss\.fffffff");
            }
            else
            {
                ProcessScenario(template, testResults, processedTestIds);
            }
        }
    }

    public static void ProcessScenario(Scenario scenario, Dictionary<string, List<UnitTestResult>> testResults, HashSet<string> processedTestIds)
    {
        // Skip if scenario already has a non-zero duration
        if (!IsZeroDuration(scenario.TestDuration))
        {
            return;
        }

        var matchedResult = TestRunExtensions.FindMatchingTestResult(scenario, testResults);

        if (matchedResult != null)
        {
            if (processedTestIds.Contains(matchedResult.TestId))
            {
                return;
            }

            StatusApplicationHelper.ApplyTestStatusToScenario(scenario, matchedResult);

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
    public static string ExtractScenarioNameFromTestName(string testName)
    {
        string name = testName;

        if (name.Contains("."))
        {
            name = name.Split('.').Last();
        }

        name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

        return name.ToLowerInvariant();
    }

    private static bool IsZeroDuration(string duration)
    {
        if (string.IsNullOrEmpty(duration))
        {
            return true;
        }

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