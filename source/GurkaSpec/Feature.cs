
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
        public string Background { get; set; }

        private TimeSpan testDuration;
        [XmlIgnore]
        public TimeSpan TestDurationTime { 
            get {
                testDuration = TimeSpan.Zero;
                foreach (var scenario in Scenarios)
                {
                    testDuration = testDuration.Add(scenario.TestDurationTime);
                }

                foreach (var rule in Rules)
                {
                    testDuration = testDuration.Add(rule.TestDurationTime);
                }

                return testDuration;
            }
            set { }
        }

        public string TestDuration { get { return TestDurationTime.ToString("G"); }
            set { } }

        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();
        public Scenario GetScenario(string name)
        {
            Scenario? scenario = Scenarios.FirstOrDefault(s => s.Name == name);
            if(scenario == null)
            {
                foreach (var rule in Rules)
                {
                    var ruleScenario = rule.GetScenario(name);
                    if (ruleScenario != null)
                    {
                        return ruleScenario;
                    }
                }
            }
            return scenario;
        }

        public List<Rule> Rules { get; set; } = new List<Rule>();

        public Rule GetRule(string name)
        {
            var rule = Rules.FirstOrDefault(s => s.Name == name);
            return rule;
        }
    }
}
