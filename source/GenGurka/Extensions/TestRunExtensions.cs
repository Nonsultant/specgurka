using SpecGurka.GurkaSpec;
using TrxFileParser.Models;
using System;
using System.Linq;

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

                if (sceUnderTest is null)
                {
                    continue;
                }
                if (TimeSpan.TryParse(sceUnderTest.TestDuration as string, out TimeSpan currentDuration) &&
                    TimeSpan.TryParse(utr.Duration as string, out TimeSpan additionalDuration))
                {
                    TimeSpan totalDuration = currentDuration + additionalDuration;
                    sceUnderTest.TestDuration = totalDuration.ToString(@"hh\:mm\:ss\.fffffff");
                }

                feature.ParseTestOutput(utr.Output.StdOut, sceUnderTest);
            }
        }

        foreach (var feature in gurkaProject.Features)
        {
            foreach (var rule in feature.Rules)
            {
                string ruleDuration = rule.TestDuration;
            }
        }

        foreach (var feature in gurkaProject.Features)
        {
            string featureDuration = feature.TestDuration;
        }

        string productDuration = gurkaProject.TestDuration;

        gurkaProject.TestDuration = productDuration;
    }
}