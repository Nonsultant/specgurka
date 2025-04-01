namespace VizGurka.Helpers;

public class FeatureFileRepositorySettings
{
    public string BaseUrl { get; set; }
}

public class GithubSettings
{
    public string BaseUrl { get; set; }
    public string Owner { get; set; }
    public List<RepositorySettings> Repositories { get; set; }
}


public class AzureSettings
{
    public string BaseUrl { get; set; }
    public string Owner { get; set; }
    public List<RepositorySettings> Repositories { get; set; }
}

public class TagPatternsSettings
{
    public GithubSettings Github { get; set; }
    public AzureSettings Azure { get; set; }
    
}

public class RepositorySettings
{
    public string Name { get; set; }
    public List<string> Product { get; set; }
}

