namespace SpecGurka.GurkaSpec;

public class Product
{
    public required string Name { get; set; }



    public bool TestsPassed
    {
        get
        {
            bool testsPassed = true;
            foreach (var feature in Features)
            {
                if (feature.Status == Status.Failed)
                {
                    testsPassed = false;
                    break;
                }
            }

            return testsPassed;
        }
        set { }
    }

    private TimeSpan _testDuration;
    public string TestDuration
    {
        get
        {
            _testDuration = TimeSpan.Zero;
            foreach (var feature in Features)
            {
                _testDuration = _testDuration.Add(TimeSpan.Parse(feature.TestDuration));
            }

            return _testDuration.ToString();
        }
        set => _testDuration = TimeSpan.Parse(value);
    }

    public List<Feature> Features { get; set; } = [];
}