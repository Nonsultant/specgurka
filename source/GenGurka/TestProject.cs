using System.Configuration;
using SpecGurka.GenGurka.Exceptions;

namespace SpecGurka.GenGurka;

internal static class TestProject
{
    public static string FeaturesDirectory => ConfigurationManager.AppSettings["features-directory"]
                                       ?? throw new ConfigurationSettingException("features directory");

    public static string AssemblyFile => ConfigurationManager.AppSettings["assembly-file"]
                                       ?? throw new ConfigurationSettingException("assembly file");

    public static string TestResultFile => ConfigurationManager.AppSettings["trx-file"]
                                       ?? throw new ConfigurationSettingException("trx file");

    public static string OutputPath => ConfigurationManager.AppSettings["output-path"]
                                       ?? throw new ConfigurationSettingException("output path");
}