using SpecGurka.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SpecGurka.Config;

public class MondayConfig : IBaseConfig, IBoardConfig, IFieldIDConfig
{
    private readonly string featureBoardId;
    private readonly string userStoryBoardId;
    private readonly string specflowStatusId; 
    private readonly string fileLinkId; 
    private readonly string featureBoardTitle;
    private readonly string userStoryBoardTitle;
    private readonly string authToken;
    private readonly string baseUrl;

    public MondayConfig(IConfiguration config)
    {
        if (config == null)
            return;
        
        featureBoardId = config.GetValue<string>("monday:featureBoardId");
        userStoryBoardId = config.GetValue<string>("monday:userStoryBoardId");
        specflowStatusId = config.GetValue<string>("monday:specflowStatusId");
        fileLinkId = config.GetValue<string>("monday:fileLinkId");
        featureBoardTitle = config.GetValue<string>("monday:featureCategoryId");
        userStoryBoardTitle = config.GetValue<string>("monday:userStoryCategoryId");
        authToken = config.GetValue<string>("monday:authToken");
        baseUrl = config.GetValue<string>("monday:baseUrl");
    }

    public string FeatureBoardId { get { return featureBoardId; } }
    public string UserStoryBoardId { get { return userStoryBoardId; } }
    public string SpecflowStatusId { get { return specflowStatusId; } }
    public string FileLinkId { get { return fileLinkId; } }
    public string FeatureBoardTitle { get { return featureBoardTitle; } }
    public string UserStoryBoardTitle { get { return userStoryBoardTitle; } }
    public string AuthToken { get { return authToken; } }
    public string BaseUrl { get { return baseUrl; } }
}
