using System.Text.RegularExpressions;
using SpecGurka.GurkaSpec;

namespace VizGurka.Helpers;

public static class TestrunReader
{
    public static List<string> GetUniqueProductNames()
    {
        string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GurkaFiles");
        string[] filePaths = Directory.GetFiles(directoryPath);

        var uniqueProductNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in filePaths)
        {
            string fileName = Path.GetFileName(file);
            Regex regex = new Regex(@"_(?<Date>\d{4}-\d{2}-\d{2}T\d{2}_\d{2}_\d{2})\.gurka");
            var result = regex.Match(fileName);

            if (result.Success)
            {
                string filePath = Path.Combine(directoryPath, fileName);
                Testrun testRun = Gurka.ReadGurkaFile(filePath);

                foreach (var product in testRun.Products)
                {
                    uniqueProductNames.Add(product.Name);
                }
            }
        }

        // TODO: Remove this
        Console.WriteLine("Unique Product Names:");
        foreach (var name in uniqueProductNames)
        {
            Console.WriteLine(name);
        }

        return uniqueProductNames.ToList();
    }

    public static Testrun ReadLatestRun(string productName)
    {
        string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GurkaFiles");
        string[] filePaths = Directory.GetFiles(directoryPath);

        Testrun latestTestrun = null;
        DateTime latestDate = DateTime.MinValue;

        foreach (string file in filePaths)
        {
            string fileName = Path.GetFileName(file);
            Regex regex = new Regex(@"_(?<Date>\d{4}-\d{2}-\d{2}T\d{2}_\d{2}_\d{2})\.gurka");
            var result = regex.Match(fileName);

            if (result.Success)
            {
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
        }

        return latestTestrun;
    }
}