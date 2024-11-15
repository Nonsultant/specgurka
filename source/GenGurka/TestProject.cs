using System.Configuration;

namespace SpecGurka.GenGurka;

internal class TestProject
{
    public string FeaturesDirectory => ConfigurationManager.AppSettings["featuresdirectory"];
    public string AssemblyFile => ConfigurationManager.AppSettings["assemblyfile"];
    public string TestResultFile => ConfigurationManager.AppSettings["trxfile"];
}