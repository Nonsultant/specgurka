using SpecGurka.GurkaSpec;
using TrxFileParser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpecGurka.GenGurka.Helpers;

public static class StatusApplicationHelper
{
    // This method applied the initial status to a scenario based on ACTUAL .trx file result
    // The statuses being applied here are the truth
    public static void ApplyTestStatusToScenario(Scenario scenario, UnitTestResult result)
    {
        if (scenario.Tags.Contains("@ignore"))
        {
            return;
        }

        // Direct 1:1 mapping from TRX <outcome>
        switch (result.Outcome?.ToLowerInvariant())
        {
            case "passed":
                scenario.Status = Status.Passed;
                foreach (var step in scenario.Steps)
                {
                    step.Status = Status.Passed;
                }
                break;

            case "failed":
                scenario.Status = Status.Failed;
                foreach (var step in scenario.Steps)
                {
                    step.Status = Status.Failed;
                }
                break;

            case "notexecuted":
            default:
                scenario.Status = Status.NotImplemented;
                foreach (var step in scenario.Steps)
                {
                    step.Status = Status.NotImplemented;
                }
                break;
        }
    }
}