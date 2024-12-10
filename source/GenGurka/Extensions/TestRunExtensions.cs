using SpecGurka.GurkaSpec;
using TrxFileParser.Models;

namespace SpecGurka.GenGurka.Extensions;

internal static class TestRunExtensions
{
    internal static void MatchWithGurkaFeatures(this TestRun testRun, Product gurkaProject)
    {
            foreach (var feature in gurkaProject.Features)
            {
                if (feature.Status == Status.NotImplemented)
                {
                    feature.Scenarios.ForEach(scenario =>
                    {
                        scenario.Status = Status.NotImplemented;
                        scenario.Steps.ForEach(step => step.Status = Status.NotImplemented);
                    });
                    continue;
                }

                foreach (var utr in testRun.Results.UnitTestResults)
                {
                    var sceUnderTest = feature.GetScenario(utr.TestName);

                    if (sceUnderTest is null) continue;

                    if (utr.Outcome == "NotExecuted")
                    {
                        sceUnderTest.Status = Status.NotImplemented;
                        sceUnderTest.Steps.ForEach(step => step.Status = Status.NotImplemented);
                        continue;
                    }

                    sceUnderTest.Status = utr.Outcome == "Passed" ? Status.Passed : Status.Failed;
                    sceUnderTest.TestDuration = utr.Duration;
                    feature.ParseTestOutput(utr.Output.StdOut);
                }
            }
    }
}