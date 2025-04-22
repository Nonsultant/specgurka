namespace VizGurka.Models
{
    public class TagViewModel
    {
        public string Tag { get; set; } = string.Empty;
        public string GithubBaseUrl { get; set; } = string.Empty;
        public string GithubOwner { get; set; } = string.Empty;
        public string GithubRepoName { get; set; } = string.Empty;
        public string AzureBaseUrl { get; set; } = string.Empty;
        public string AzureRepoName { get; set; } = string.Empty;
    }
}