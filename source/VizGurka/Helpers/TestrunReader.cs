using System.Text.RegularExpressions;
using SpecGurka.GurkaSpec;

namespace VizGurka.Helpers;

public static class TestrunReader
{
    public static Testrun ReadLatestRun(string productName)
    {
        string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GurkaFiles");
        string[] filePaths = Directory.GetFiles(directoryPath);

        List<string> fileNames = new List<string>();

        foreach (string file in filePaths)
        {
            string fileName = Path.GetFileName(file);
            fileNames.Add(fileName);
        }

        Regex regex = new Regex(@"_(?<Date>\d{4}-\d{2}-\d{2}T\d{2}_\d{2}_\d{2})\.gurka");

        var fileDateDictionary = new Dictionary<string, DateTime>();
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

                fileDateDictionary.Add(fileName, dateTime);
                dates.Add(dateTime);
            }
        }

        Testrun latestTestrunOne = null;
        Testrun latestTestrunTwo = null;
        DateTime latestDateOne = DateTime.MinValue;
        DateTime latestDateTwo = DateTime.MinValue;

        foreach (var fileName in fileDateDictionary.Keys)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GurkaFiles/{fileName}");
            Testrun testRun = Gurka.ReadGurkaFile(filePath);

            foreach (var product in testRun.Products)
            {
                if (product.Name == "One" && fileDateDictionary[fileName] > latestDateOne)
                {
                    latestDateOne = fileDateDictionary[fileName];
                    latestTestrunOne = testRun;
                }
                else if (product.Name == "Two" && fileDateDictionary[fileName] > latestDateTwo)
                {
                    latestDateTwo = fileDateDictionary[fileName];
                    latestTestrunTwo = testRun;
                }
            }
        }

        return productName == "One" ? latestTestrunOne : latestTestrunTwo;
    }
}