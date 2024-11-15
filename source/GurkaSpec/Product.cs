using System.Xml.Serialization;

namespace SpecGurka.GurkaSpec;

public class Product
{
    public string Name { get; set; }
    public bool TestsPassed { get; set; }

    private TimeSpan testDuration;
        
    [XmlIgnore]
    public TimeSpan TestDurationTime
    {
        get
        {
            testDuration = TimeSpan.Zero;
            foreach (var feature in Features)
            {
                testDuration = testDuration.Add(feature.TestDurationTime);
            }
            return testDuration;
        }
        set { }
    }

    public string TestDuration
    {
        get { return TestDurationTime.ToString("G"); }
        set { }
    }

    public List<Feature> Features { get; set; } = new List<Feature>();

    public Feature GetFeature(string name)
    {
        var feature = Features.FirstOrDefault(f => f.Name == name);
        return feature;
    }
}