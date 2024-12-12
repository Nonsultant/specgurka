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
                var sceUnderTest = feature.GetScenario(utr);

                if (sceUnderTest is null || utr.Outcome == "NotExecuted") continue;

                if (TimeSpan.TryParse(sceUnderTest.TestDuration as string, out TimeSpan currentDuration) &&
                    TimeSpan.TryParse(utr.Duration as string, out TimeSpan additionalDuration))
                {
                    sceUnderTest.TestDuration = (currentDuration + additionalDuration).ToString();
                }

                feature.ParseTestOutput(utr.Output.StdOut, sceUnderTest);
            }
        }
    }
}