using SpecGurka.GurkaSpec;
using TrxFileParser.Models;

namespace SpecGurka.GenGurka.Extensions;

internal static class TestRunExtensions
{
    internal static void MatchWithGurkaFeatures(this TestRun testRun, Product gurkaProject)
    {
        var sortedTestResults = testRun.Results.UnitTestResults
            .OrderBy(utr => gurkaProject.Features.FindIndex(f => f.GetScenario(utr.TestName) != null))
            .ToList();

            foreach (var feature in gurkaProject.Features)
            {
                foreach (var utr in sortedTestResults)
                {
                    var sceUnderTest = feature.GetScenario(utr.TestName);

                    if (sceUnderTest is not null)
                    {
                        sceUnderTest.TestsPassed = utr.Outcome == "Passed";
                        sceUnderTest.TestDuration = utr.Duration;
                        feature.ParseTestOutput(utr.Output.StdOut);
                        if (utr.Output.ErrorInfo != null)
                        {
                            sceUnderTest.ErrorMessage = utr.Output.ErrorInfo.Message;
                        }
                    }
                    else
                    {
                        var bgUnderTest = feature.GetBackground(utr.TestName);
                        if (bgUnderTest is not null)
                        {
                            bgUnderTest.TestsPassed = utr.Outcome == "Passed";
                            bgUnderTest.TestDuration = utr.Duration;
                            feature.ParseTestOutput(utr.Output.StdOut);
                            if (utr.Output.ErrorInfo != null)
                            {
                                bgUnderTest.ErrorMessage = utr.Output.ErrorInfo.Message;
                            }
                        }
                    }
                }
            }
    }
}