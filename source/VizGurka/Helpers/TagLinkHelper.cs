namespace VizGurka.Helpers;

public class FeatureFileRepositorySettings
{
    public string BaseUrl { get; set; } = string.Empty;
}

public class GithubSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<RepositorySettings> Repositories { get; set; } = new List<RepositorySettings>();
}


public class AzureSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<RepositorySettings> Repositories { get; set; } = new List<RepositorySettings>();
}

public class TagPatternsSettings
{
    public GithubSettings Github { get; set; } = new GithubSettings();
    public AzureSettings Azure { get; set; } = new AzureSettings();
    
}

public class RepositorySettings
{
    public string Name { get; set; } = string.Empty;
    public List<string> Product { get; set; } = new List<string>();
}

