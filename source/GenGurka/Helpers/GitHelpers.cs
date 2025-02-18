using System.Diagnostics;

namespace SpecGurka.GenGurka.Helpers
{
    public static class GitHelpers
    {
        public static string GetLatestCommitId(string repositoryPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "log -1 --pretty=\"%H\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = repositoryPath
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string commitId = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return commitId;
            }
        }

        public static string GetLatestCommitAuthor(string repositoryPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "log -1 --pretty=\"%an\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = repositoryPath
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string commitAuthor = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return commitAuthor;
            }
        }

        public static string GetBranchName(string repositoryPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "branch --show-current",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = repositoryPath
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string branchName = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return branchName;
            }
        }
    }
}