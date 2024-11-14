using SpecGurka.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SpecGurka.Config;

public class AzureDevOpsConfig : IBaseConfig, IOwnerProjectConfig, IFieldIDConfig
{   
    private readonly string owner = string.Empty;
    private readonly string project = string.Empty;
    private readonly string specflowStatusId = string.Empty;
    private readonly string fileLinkId = string.Empty;
    private readonly string baseUrl = string.Empty;
    private readonly string authToken = string.Empty;

    public AzureDevOpsConfig(IConfiguration config)
    {
        if (config == null)
            return;

        owner = config.GetValue<string>("azureDevOps:organization");
        project = config.GetValue<string>("azureDevops:project");
        specflowStatusId = config.GetValue<string>("azureDevops:specflowStatusId");
        fileLinkId = config.GetValue<string>("azureDevops:fileLinkId");
        baseUrl = config.GetValue<string>("azureDevops:baseUrl");
        authToken = config.GetValue<string>("azureDevops:authToken");
    }

    public string Owner { get { return owner; } }
    public string Project { get { return project; } }
    public string SpecflowStatusId { get { return specflowStatusId; } }
    public string FileLinkId { get { return fileLinkId; } }
    public string BaseUrl { get { return baseUrl; } }
    public string AuthToken { get { return authToken; } }
}
