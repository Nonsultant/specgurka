using SpecGurka.GurkaSpec;
using TrxFileParser.Models;

namespace SpecGurka.GenGurka.Extensions;

internal static class TestRunExtensions
{
    internal static void MatchWithGurkaFeatures(this TestRun testRun, Product gurkaProject)
    {
        foreach (var feature in gurkaProject.Features)
        {
            foreach (var utr in testRun.Results.UnitTestResults)
            {
                var sceUnderTest = feature.GetScenario(utr.TestName);

                if (sceUnderTest is null || utr.Outcome == "NotExecuted") continue;

                sceUnderTest.TestDuration = utr.Duration;
                feature.ParseTestOutput(utr.Output.StdOut, sceUnderTest);
            }
        }
    }
}