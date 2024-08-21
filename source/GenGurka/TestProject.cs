using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecGurka.GenGurka
{
    internal class TestProject
    {
        public string GherkinFile { get { return ConfigurationManager.AppSettings["gherkinfile"]; } }
        public string AssemblyFile { get { return ConfigurationManager.AppSettings["assemblyfile"]; } }

        public string TestResultFile { get { return ConfigurationManager.AppSettings["trxfile"]; } }

        


    }
}
