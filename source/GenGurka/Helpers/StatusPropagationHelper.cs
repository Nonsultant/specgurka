
using SpecGurka.GurkaSpec;
using TrxFileParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpecGurka.GenGurka.Helpers;

public static class StatusPropagationHelper
{
    // This method handles the propagation of statuses. This modifies the truth state of the statuses
    // But propagation happens no where else in the codebase (I hope)
    public static void PropagateStatusesToParents(Product gurkaProject)
    {
        foreach (var feature in gurkaProject.Features)
        {
            foreach (var rule in feature.Rules)
            {
                if (rule.Tags.Contains("@ignore"))
                {
                    rule.Status = Status.NotImplemented;

                    foreach (var scenario in rule.Scenarios)
                    {
                        if (!scenario.Tags.Contains("@ignore"))
                        {
                            scenario.Tags.Add("@ignore");
                        }

                        scenario.Status = Status.NotImplemented;

                        foreach (var step in scenario.Steps)
                        {
                            step.Status = Status.NotImplemented;
                        }
                    }
                }
            }

            if (feature.Tags.Contains("@ignore"))
            {
                feature.Status = Status.NotImplemented;

                foreach (var rule in feature.Rules)
                {
                    if (!rule.Tags.Contains("@ignore"))
                    {
                        rule.Tags.Add("@ignore");
                    }

                    rule.Status = Status.NotImplemented;

                    foreach (var scenario in rule.Scenarios)
                    {
                        if (!scenario.Tags.Contains("@ignore"))
                        {
                            scenario.Tags.Add("@ignore");
                        }

                        scenario.Status = Status.NotImplemented;

                        foreach (var step in scenario.Steps)
                        {
                            step.Status = Status.NotImplemented;
                        }
                    }
                }

                foreach (var scenario in feature.Scenarios)
                {
                    if (!scenario.Tags.Contains("@ignore"))
                    {
                        scenario.Tags.Add("@ignore");
                    }

                    scenario.Status = Status.NotImplemented;

                    foreach (var step in scenario.Steps)
                    {
                        step.Status = Status.NotImplemented;
                    }
                }

                return;
            }

            foreach (var rule in feature.Rules)
            {
                UpdateRuleStatus(rule);
            }

            UpdateFeatureStatus(feature);
        }
    }

    public static void UpdateRuleStatus(Rule rule)
    {
        if (rule.Tags.Contains("@ignore"))
        {
            foreach (var scenario in rule.Scenarios)
            {
                if (!scenario.Tags.Contains("@ignore"))
                {
                    scenario.Tags.Add("@ignore");
                }

                scenario.Status = Status.NotImplemented;

                foreach (var step in scenario.Steps)
                {
                    step.Status = Status.NotImplemented;
                }
            }

            rule.Status = Status.NotImplemented;
            return;
        }

        if (rule.Scenarios.Any(s => !s.Tags.Contains("@ignore") && s.Status == Status.Failed) ||
            (rule.Background != null && rule.Background.Status == Status.Failed))
        {
            rule.Status = Status.Failed;
            return;
        }

        var nonIgnoredScenarios = rule.Scenarios.Where(s => !s.Tags.Contains("@ignore")).ToList();
        if (nonIgnoredScenarios.Any() && nonIgnoredScenarios.All(s => s.Status == Status.Passed))
        {
            rule.Status = Status.Passed;
            return;
        }

        rule.Status = Status.NotImplemented;
    }

    public static void UpdateFeatureStatus(Feature feature)
    {
        if (feature.Tags.Contains("@ignore"))
        {
            return;
        }

        if ((feature.Scenarios.Count == 0 || feature.Scenarios.All(s => s.Tags.Contains("@ignore"))) &&
            (feature.Rules.Count == 0 || feature.Rules.All(r => r.Tags.Contains("@ignore"))))
        {
            feature.Status = Status.NotImplemented;
            return;
        }

        if (feature.Scenarios.Any(s => !s.Tags.Contains("@ignore") && s.Status == Status.Failed) ||
            feature.Rules.Any(r => !r.Tags.Contains("@ignore") && r.Status == Status.Failed) ||
            (feature.Background != null && feature.Background.Status == Status.Failed))
        {
            feature.Status = Status.Failed;
            return;
        }

        bool hasNonIgnoredItems =
            feature.Scenarios.Any(s => !s.Tags.Contains("@ignore")) ||
            feature.Rules.Any(r => !r.Tags.Contains("@ignore"));

        bool allNonIgnoredPassed =
            feature.Scenarios.Where(s => !s.Tags.Contains("@ignore")).All(s => s.Status == Status.Passed) &&
            feature.Rules.Where(r => !r.Tags.Contains("@ignore")).All(r => r.Status == Status.Passed);

        if (hasNonIgnoredItems && allNonIgnoredPassed)
        {
            feature.Status = Status.Passed;
            return;
        }

        feature.Status = Status.NotImplemented;
    }

    public static void ApplyStepStatuses(Scenario scenario, string output)
    {
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

    public static void MarkIgnoredItemsStatus(GurkaSpec.Feature gurkaFeature)
    {
        // Set initial status for ignored items, but don't cascade

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