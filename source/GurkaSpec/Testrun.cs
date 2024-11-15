using System.Globalization;

namespace SpecGurka.GurkaSpec;

public class Testrun
{
    public required string Name { get; set; }

    private TimeSpan _totalTestDuration;
    public string TotalTestDuration
    {
        get
        {
            _totalTestDuration = TimeSpan.Zero;
            foreach (var product in Products)
            {
                _totalTestDuration = _totalTestDuration.Add(TimeSpan.Parse(product.TestDuration));
            }
            return _totalTestDuration.ToString();
        }
        set => _totalTestDuration = TimeSpan.Parse(value);
    }

    private DateTime _date;
    public string Date
    {
        get => _date.ToString(CultureInfo.InvariantCulture);
        set => _date = DateTime.Parse(value);
    }

    public List<Product> Products { get; set; } = [];
}