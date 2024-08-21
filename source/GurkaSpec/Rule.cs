using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpecGurka.GurkaSpec
{
    public class Rule
    {
        public string Name { get; set; }

        public bool TestsPassed { get
            {
                bool testsPassed = true;
                foreach(var scenario in Scenarios)
                {
                    if (!scenario.TestPassed)
                    {
                        testsPassed = false;
                        break;
                    }
                }

                return testsPassed;
            }
            set { } }

        private TimeSpan testDuration;
        [XmlIgnore]
        public TimeSpan TestDurationTime
        {
            get
            {
                testDuration = TimeSpan.Zero;
                foreach (var scenario in Scenarios)
                {
                    testDuration = testDuration.Add(scenario.TestDurationTime);
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

        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();

        public Scenario GetScenario(string name)
        {
            var scenario = Scenarios.FirstOrDefault(s => s.Name == name);
            return scenario;
        }
    }
}
