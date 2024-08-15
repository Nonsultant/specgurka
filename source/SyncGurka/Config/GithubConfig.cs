using SpecGurka.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SpecGurka.Config;

public class GithubConfig : IBaseConfig, IOwnerProjectConfig
{
    private readonly string authToken = string.Empty;
    private readonly string baseUrl = string.Empty;
    private readonly string owner = string.Empty;
    private readonly string repo = string.Empty;

    public GithubConfig(IConfiguration config)
    {
        if (config == null)
            return;

        authToken = config.GetValue<string>("github:authToken");
        baseUrl = config.GetValue<string>("github:baseUrl");
        owner = config.GetValue<string>("github:owner");
        repo = config.GetValue<string>("github:repo");
    }

    public string AuthToken { get { return authToken; } }
    public string BaseUrl { get { return baseUrl; } }
    public string Owner { get { return owner; } }
    public string Project {  get { return repo; } }
}
