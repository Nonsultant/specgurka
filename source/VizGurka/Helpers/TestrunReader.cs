using System;
using System.IO;
using System.Text.RegularExpressions;
using SpecGurka.GurkaSpec;
using VizGurka.Services;

namespace VizGurka.Helpers;

public static class TestrunReader
{
    public static List<string> GetUniqueProductNames()
    {
        SupabaseService.SyncFilesFromSupabase().GetAwaiter().GetResult();

        string directoryPath = "../VizGurka/GurkaFiles";
        string[] filePaths = Directory.GetFiles(directoryPath);

        var uniqueProductNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in filePaths)
        {
            string fileName = Path.GetFileName(file);
            Regex regex = new Regex(@"_(?<Date>\d{4}-\d{2}-\d{2}T\d{2}_\d{2}_\d{2})\.gurka");
            var result = regex.Match(fileName);

            if (!result.Success)
            {
                continue;
            }

            string filePath = Path.Combine(directoryPath, fileName);
            AddProductNamesToSet(filePath, uniqueProductNames);
        }

        return uniqueProductNames.ToList();
    }

    private static List<string> ExtractProductNamesFromFile(string filePath)
    {
        var productNames = new List<string>();
        Testrun testRun = Gurka.ReadGurkaFile(filePath);

        foreach (var product in testRun.Products)
        {
            productNames.Add(product.Name);
        }

        return productNames;
    }

    private static void AddProductNamesToSet(string filePath, HashSet<string> uniqueProductNames)
    {
        var productNames = ExtractProductNamesFromFile(filePath);

        foreach (var productName in productNames)
        {
            uniqueProductNames.Add(productName);
        }
    }


    public static Testrun? ReadLatestRun(string productName)
    {
        string directoryPath = "../VizGurka/GurkaFiles";
        string[] filePaths = Directory.GetFiles(directoryPath);

        Testrun? latestTestrun = null;
        DateTime latestDate = DateTime.MinValue;

        foreach (string file in filePaths)
        {
            string fileName = Path.GetFileName(file);
            Regex regex = new Regex(@"_(?<Date>\d{4}-\d{2}-\d{2}T\d{2}_\d{2}_\d{2})\.gurka");
            var result = regex.Match(fileName);

            if (!result.Success)
            {
                continue;
            }

            string date = result.Groups["Date"].Value;
            date = date.Replace('_', ':');
            var dateTime = DateTime.Parse(date);

            string filePath = Path.Combine(directoryPath, fileName);
            Testrun testRun = Gurka.ReadGurkaFile(filePath);

            foreach (var product in testRun.Products)
            {
                if (string.Equals(product.Name, productName, StringComparison.OrdinalIgnoreCase) && dateTime > latestDate)
                {
                    latestDate = dateTime;
                    latestTestrun = testRun;
                }
            }
        }

        return latestTestrun;
    }
}