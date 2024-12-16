using System.Diagnostics;

namespace SpecGurka.GenGurka.Helpers;

public static class DotNetTestRunner
{
    public static void Run()
    {
        using (var process = new Process())
        {
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = "test --logger trx --results-directory .";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            Console.WriteLine("Starting test run...");
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            Console.WriteLine("Finished test run...");
        }
    }
}