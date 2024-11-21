using SpecGurka.GurkaSpec;
using TrxFileParser.Models;

namespace SpecGurka.GenGurka.Extensions;

public static class TestRunExtensions
{
    public static void MatchWithGurkaFeatures(this TestRun testRun, Product gurkaProject)
    {
        var sortedTestResults = testRun.Results.UnitTestResults
            .OrderBy(utr => gurkaProject.Features.FindIndex(f => f.GetScenario(utr.TestName) != null))
            .ToList();

        foreach (var feature in gurkaProject.Features)
        {
            bool featurePassed = true;
            foreach (var utr in sortedTestResults)
            {
                var sceUnderTest = feature.GetScenario(utr.TestName);
                if (sceUnderTest == null)
                    continue;
                bool outcome = utr.Outcome == "Passed";
                sceUnderTest.TestPassed = outcome;
                sceUnderTest.TestOutput = utr.Output.StdOut;
                sceUnderTest.TestDuration = utr.Duration;
                sceUnderTest.ParseTestOutput(utr.Output.StdOut);
                if (utr.Output.ErrorInfo != null)
                {
                    sceUnderTest.ParseTestError(utr.Output.ErrorInfo.Message);
                    sceUnderTest.ErrorMessage = utr.Output.ErrorInfo.Message;
                }
                if (!outcome)
                    featurePassed = false;
            }
            feature.TestsPassed = featurePassed;
        }
    }
}