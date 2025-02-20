using System.Diagnostics;

namespace SpecGurka.GenGurka.Helpers
{
    public static class GitHelpers
    {
        public static string GetBranchName(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "rev-parse --abbrev-ref HEAD");
        }
        public static string GetLatestCommitId(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "log -1 --pretty=\"%H\"");
        }

        public static string GetLatestCommitAuthor(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "log -1 --pretty=\"%an\"");
        }

        public static string GetLatestCommitDate(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "log -1 --pretty=\"%ad\" --date=iso-strict");
        }

        public static string GetLatestCommitMessage(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "log -1 --pretty=\"%s\"");
        }

        public static string GetLatestTag(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "describe --tags --abbrev=0");
        }

        public static string GetCommitCount(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "rev-list --count HEAD");
        }

        private static string ExecuteGitCommand(string repositoryPath, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = repositoryPath
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return output;
            }
        }
    }
}