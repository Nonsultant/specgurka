using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using SpecGurka.Specflow;
using Gherkin.Ast;

namespace SpecGurka
{
    public class Feature
    {
        private readonly GherkinFileService gherkinFileService;
        private readonly ISystemClient serviceClient;
        private readonly IWorkItemService workItemService;
        private readonly ISystemFileLinkService systemFileLinkService;
        private readonly ISystemSpecflowService systemSpecflowService;
        private readonly ISystemUserStoryService systemUserStoryService;

        public Feature(GherkinFileService gherkinFileService, ISystemClient serviceClient,
            IWorkItemService workItemService, ISystemFileLinkService systemFileLinkService, 
            ISystemSpecflowService systemSpecflowService, ISystemUserStoryService systemUserStoryService)
        {
            this.gherkinFileService = gherkinFileService;
            this.serviceClient = serviceClient;
            this.workItemService = workItemService;
            this.systemFileLinkService = systemFileLinkService;
            this.systemSpecflowService = systemSpecflowService;
            this.systemUserStoryService = systemUserStoryService;
        }

        public string GherkinFilePath { get; set; }
        public GherkinDocument GherkinFileContent { get; set; }
        public WorkItem ServiceFeatureItem { get; set; }
        public SpecflowFeatureResult SpecflowResult { get; set; }

        public void VerifyGherkinFile()
        {
            gherkinFileService.VerifyGherkinId(GherkinFileContent);

            gherkinFileService.CheckIfFeatureIdsOnScenarioLevel(GherkinFileContent);

            gherkinFileService.VerifyGherkinFeatureTitle(GherkinFileContent);

            gherkinFileService.VerifyGherkinFileNameAndTitle(GherkinFileContent, GherkinFilePath);
        }

        public async Task FetchFeatureItemFromService()
        {
            string id = gherkinFileService.GetFeatureId(GherkinFileContent);
            ServiceFeatureItem = await serviceClient.GetWorkItemFromSystem(id);
        }

        public void VerifyFeatureItemFromService()
        {
            workItemService.VerifyWorkItemIsOfTypeFeature(ServiceFeatureItem);
            workItemService.VerifyGherkinTitleAndWorkItemTitleAreTheSame(GherkinFileContent, ServiceFeatureItem);
            systemFileLinkService.VerifyGherkinFileLinkOnSystem(GherkinFilePath, ServiceFeatureItem);
        }

        public async Task UpdateServiceItemWithSpecflowResult()
        {   
            await systemSpecflowService.UpdateSystemWithSpecflowResults(ServiceFeatureItem, SpecflowResult);
        }

        public bool IsSpecFlowStatusUpToDate()
        {
            if (ServiceFeatureItem.SpecflowStatus == "OK" && SpecflowResult.AllScenariosAreOK())
                return true;

            if (ServiceFeatureItem.SpecflowStatus == "NOK" && !SpecflowResult.AllScenariosAreOK())
                return true;

            return false;
        }
    }
}
