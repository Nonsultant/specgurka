
using SpecGurka.GurkaSpec;
using TrxFileParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpecGurka.GenGurka.Helpers;

public static class StatusPropagationHelper
{
    public static void PropagateStatusesToParents(Product gurkaProject)
    {
        foreach (var feature in gurkaProject.Features)
        {
            if (feature.Tags.Contains("@ignore"))
            {
                SetFeatureNotImplemented(feature);
                continue;
            }
            
            foreach (var rule in feature.Rules)
            {
                if (rule.Tags.Contains("@ignore"))
                {
                    SetRuleNotImplemented(rule);
                }
                else
                {
                    UpdateRuleStatus(rule);
                }
            }
            
            foreach (var scenario in feature.Scenarios)
            {
                if (scenario.Tags.Contains("@ignore"))
                {
                    SetScenarioNotImplemented(scenario);
                }
            }
            
            UpdateFeatureStatus(feature);
        }
    }

    private static void SetFeatureNotImplemented(Feature feature)
    {
        feature.Status = Status.NotImplemented;
        foreach (var rule in feature.Rules)
        {
            SetRuleNotImplemented(rule);
        }
        foreach (var scenario in feature.Scenarios)
        {
            SetScenarioNotImplemented(scenario);
        }
    }

    private static void SetRuleNotImplemented(Rule rule)
    {
        rule.Status = Status.NotImplemented;
        foreach (var scenario in rule.Scenarios)
        {
            SetScenarioNotImplemented(scenario);
        }
    }

    private static void SetScenarioNotImplemented(Scenario scenario)
    {
        scenario.Status = Status.NotImplemented;
        foreach (var step in scenario.Steps)
        {
            step.Status = Status.NotImplemented;
        }
    }

    public static void UpdateRuleStatus(Rule rule)
    {
        if (rule.Status == Status.NotImplemented) return;
        
        var hasFailed = rule.Scenarios.Any(s => s.Status == Status.Failed) ||
                       (rule.Background?.Status == Status.Failed);

        if (hasFailed)
        {
            rule.Status = Status.Failed;
            return;
        }
        
        var allPassed = rule.Scenarios.All(s => s.Status == Status.Passed) &&
                       (rule.Background == null || rule.Background.Status == Status.Passed);

        rule.Status = allPassed ? Status.Passed : Status.NotImplemented;
    }

    public static void UpdateFeatureStatus(Feature feature)
    {
        if (feature.Status == Status.NotImplemented) return;
        
        var hasFailed = feature.Rules.Any(r => r.Status == Status.Failed) ||
                        feature.Scenarios.Any(s => s.Status == Status.Failed) ||
                        (feature.Background?.Status == Status.Failed);

        if (hasFailed)
        {
            feature.Status = Status.Failed;
            return;
        }
        
        var hasNotImplemented = feature.Rules.Any(r => r.Status == Status.NotImplemented) ||
                                feature.Scenarios.Any(s => s.Status == Status.NotImplemented);

        if (hasNotImplemented)
        {
            feature.Status = Status.NotImplemented;
            return;
        }
        
        var allPassed = feature.Rules.All(r => r.Status == Status.Passed) &&
                        feature.Scenarios.All(s => s.Status == Status.Passed) &&
                        (feature.Background == null || feature.Background.Status == Status.Passed);

        feature.Status = allPassed ? Status.Passed : Status.NotImplemented;
    }

    public static void ApplyStepStatuses(Scenario scenario, string output)
    {
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int currentStepIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            
            for (int stepIdx = 0; stepIdx < scenario.Steps.Count; stepIdx++)
            {
                var step = scenario.Steps[stepIdx];
                if (line.Contains(step.Text))
                {
                    currentStepIndex = stepIdx;
                    break;
                }
            }

            if (currentStepIndex >= 0 && currentStepIndex < scenario.Steps.Count)
            {
                var currentStep = scenario.Steps[currentStepIndex];

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

    public static void MarkIgnoredItemsStatus(GurkaSpec.Feature gurkaFeature)
    {
        if (gurkaFeature.Tags.Contains("@ignore"))
        {
            gurkaFeature.Status = Status.NotImplemented;
        }

        if (gurkaFeature.Background != null && gurkaFeature.Tags.Contains("@ignore"))
        {
            gurkaFeature.Background.Status = Status.NotImplemented;
        }

        foreach (var scenario in gurkaFeature.Scenarios)
        {
            if (scenario.Tags.Contains("@ignore"))
            {
                scenario.Status = Status.NotImplemented;
            }
        }

        foreach (var rule in gurkaFeature.Rules)
        {
            if (rule.Tags.Contains("@ignore"))
            {
                rule.Status = Status.NotImplemented;
            }
        }
    }
}
