using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpecGurka.GurkaSpec
{
    public class Scenario
    {
        public string Name { get; set; }
        public bool TestPassed { get; set; }

        [XmlIgnore]
        public TimeSpan TestDurationTime { get; set; }

        public string TestDuration
        {
            get { return TestDurationTime.ToString("G"); }
            set { }
        }

        [XmlIgnore]
        public string TestOutput { get; set; }

        [XmlIgnore]
        public string ErrorMessage { get; set; }

        public List<Step> Steps { get; set; } = new List<Step>();

        public Step GetStep(string kind, string text)
        {
            var step = Steps.FirstOrDefault(f => f.Text == text && f.Kind == kind);
            return step;
        }

        public void ParseTestOutput(string output)
        {
            var lines = output.Split('\n');
            for (int n = 0; n < lines.Length; n = n + 2)
            {
                var line1 = lines[n];
                Regex regex1 = new Regex("^(\\w+)[ ](.+)$");
                var matches1 = regex1.Matches(line1)[0];
                var kindMatch = matches1.Groups[1];
                var stepMatch = matches1.Groups[2];
                var step = GetStep(null, stepMatch.Value);

                var line2 = lines[n + 1];
                Regex regex2 = new Regex("^(.{2,5})\\s(.+):\\s(.+)\\s\\((.+)s\\)$");
                var matches2 = regex2.Matches(line2);
                if (matches2.Count == 0)
                    break;

                var match2res = matches2[0];
                step.TestPassed = match2res.Groups[2].Value == "done" ? true : false ;
                step.TestMethod = match2res.Groups[3].Value;
                step.TestDurationSeconds = match2res.Groups[4].Value.Replace(",",".");


            }
        }

        public void ParseTestError(string errorOutput)
        {
            //throw new NotImplementedException();
        }
    }

}
