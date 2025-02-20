using System.Globalization;

namespace SpecGurka.GurkaSpec;

public class Testrun
{
    public required string Name { get; set; }
    public string RepositoryUrl { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string CommitId { get; set; } = string.Empty;
    public string CommitDate { get; set; } = string.Empty;
    public string CommitAuthor { get; set; } = string.Empty;
    public string CommitMessage { get; set; } = string.Empty;

    public bool TestsPassed
    {
        get
        {
            bool testsPassed = true;
            foreach (var product in Products)
            {
                if (!product.TestsPassed)
                {
                    testsPassed = false;
                    break;
                }
            }

            return testsPassed;
        }
        set { }
    }

    private TimeSpan _totalDuration;
    public string TotalDuration
    {
        get
        {
            _totalDuration = TimeSpan.Zero;
            foreach (var product in Products)
            {
                _totalDuration = _totalDuration.Add(TimeSpan.Parse(product.TestDuration));
            }
            return _totalDuration.ToString();
        }
        set => _totalDuration = TimeSpan.Parse(value);
    }

    private DateTime _dateAndTime;
    public string DateAndTime
    {
        get => _dateAndTime.ToString(CultureInfo.InvariantCulture);
        set => _dateAndTime = DateTime.Parse(value, CultureInfo.InvariantCulture);
    }

    public List<Product> Products { get; set; } = [];
}