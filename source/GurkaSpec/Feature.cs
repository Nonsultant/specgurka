
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpecGurka.GurkaSpec
{
    public class Feature
    {
        public string Name { get; set; }
        public bool TestsPassed { get; set; }

        private TimeSpan testDuration;
        [XmlIgnore]
        public TimeSpan TestDurationTime { 
            get {
                testDuration = TimeSpan.Zero;
                foreach (var scenario in Scenarios)
                {
                    testDuration = testDuration.Add(scenario.TestDurationTime);
                }
                return testDuration;
            }
            set { }
        }

        public string TestDuration { get { return TestDurationTime.ToString("G"); }
            set { } }

        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();

        // rules

        public Scenario GetScenario(string name)
        {
            var scenario = Scenarios.FirstOrDefault(s => s.Name == name);
            return scenario;
        }
    }
}
