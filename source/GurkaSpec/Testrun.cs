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
            foreach (var feature in Features)
            {
                _totalTestDuration = _totalTestDuration.Add(TimeSpan.Parse(feature.TestDuration));
            }
            return _totalTestDuration.ToString("G");
        }
        set => _totalTestDuration = TimeSpan.Parse(value);
    }

    public List<Feature> Features { get; set; } = [];
}