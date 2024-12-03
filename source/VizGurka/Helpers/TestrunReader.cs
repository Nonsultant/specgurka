using System.Text.RegularExpressions;
using SpecGurka.GurkaSpec;

namespace VizGurka.Helpers;

public static class TestrunReader
{
    public static Testrun ReadLatestRun()
    {
        string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GurkaFiles");
        string[] filePaths = Directory.GetFiles(directoryPath);

        List<string> fileNames = [];

        foreach (string file in filePaths)
        {
            string fileName = Path.GetFileName(file);
            fileNames.Add(fileName);
        }

        Regex regex = new Regex(@"_(?<Date>\d{4}-\d{2}-\d{2}T\d{2}_\d{2}_\d{2})\.gurka");

        var dictionary = new Dictionary<string, DateTime>();
        var dates = new List<DateTime>();

        foreach (var fileName in fileNames)
        {
            var result = regex.Match(fileName);

            if (result.Success)
            {
                string date = result.Groups["Date"].Value;

                //this will convert back the date to the correct format to parse
                date = date.Replace('_', ':');

                var dateTime = DateTime.Parse(date);

                dictionary.Add(fileName, dateTime);
                dates.Add(dateTime);
            }
        }

        var latestDate = dates.OrderByDescending(d => d).First();

        string latestFileName = dictionary.FirstOrDefault(x => x.Value == latestDate).Key;

        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GurkaFiles/{latestFileName}");

        var testRun = Gurka.ReadGurkaFile(filePath);

        return testRun;
    }
}