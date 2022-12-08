using SpecGurka.Exceptions;
using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using Microsoft.Extensions.Configuration;
using SpecGurka.WorkSystems.Monday;
using SpecGurka.WorkSystems.Github;
using SpecGurka.WorkSystems.AzureDevOps;
using SpecGurka.Config;

namespace SpecGurka
{
    public class ServiceFactory
    {
        private readonly UIHelper UI;
        private readonly GherkinFileService fileService;
        private readonly IConfiguration config;

        public ServiceFactory(UIHelper UI, GherkinFileService fileService, IConfiguration config)
        {
            this.UI = UI;
            this.fileService = fileService;
            this.config = config;
        }

        public ISystemClient GetServiceClient()
        {
            var serviceType = config.GetValue<string>("serviceType");

            if (serviceType == "AzureDevOps")
            {
                AzureDevOpsConfig azureConfig = new(config);
                return new AzureDevOpsClient(UI, fileService, azureConfig);
            }
            else if (serviceType == "Monday")
            {
                MondayConfig mondayConfig = new(config); 
                return new MondayClient(UI, fileService, mondayConfig);
            }
            else if (serviceType == "Github")
            {
                GithubConfig githubConfig = new(config);
                return new GithubClient(UI, fileService, githubConfig);
            }

            throw new NotFoundException("The service type was entered incorrectly.");
        }

        public IWorkItemService GetWorkItemService()
        {
            var serviceType = config.GetValue<string>("serviceType");

            if (serviceType == "AzureDevOps")
            {
                AzureDevOpsConfig azureConfig = new(config);
                return new AzureDevOpsWorkItemService(UI, azureConfig);
            }
            else if (serviceType == "Monday")
            {
                MondayConfig mondayConfig = new(config);
                return new MondayWorkItemService(mondayConfig, UI, fileService);
            }
            else if (serviceType == "Github")
            {
                GithubConfig githubConfig = new(config);
                return new GithubWorkItemService(githubConfig, UI);
            }

            throw new NotFoundException("The service type was entered incorrectly.");
        }

        public ISystemFileLinkService GetSystemFileLinkService()
        {
            var serviceType = config.GetValue<string>("serviceType");

            if (serviceType == "AzureDevOps")
            {
                AzureDevOpsConfig azureConfig = new(config);
                AzureDevOpsClient azureClient = new(UI, fileService, azureConfig);
                return new AzureDevOpsFileLinkService(azureConfig, UI, fileService, azureClient);
            }
            else if (serviceType == "Monday")
            {
                MondayConfig mondayConfig = new(config);
                MondayClient mondayClient = new(UI, fileService, mondayConfig);
                return new MondayFileLinkService(UI, fileService, mondayClient, mondayConfig);
            }
            else if (serviceType == "Github")
            {
                GithubConfig githubConfig = new(config);
                GithubClient githubClient = new(UI, fileService, githubConfig);
                return new GithubFileLinkService(UI, fileService, githubConfig, githubClient);
            }

            throw new NotFoundException("The service type was entered incorrectly.");
        }

        public ISystemSpecflowService GetSystemSpecflowService()
        {
            var serviceType = config.GetValue<string>("serviceType");

            if (serviceType == "AzureDevOps")
            {
                AzureDevOpsConfig azureConfig = new(config);
                AzureDevOpsClient azureClient = new(UI, fileService, azureConfig);
                return new AzureDevOpsSpecflowService(azureConfig, UI, azureClient);
            }
            else if (serviceType == "Monday")
            {
                MondayConfig mondayConfig = new(config);
                MondayClient mondayClient = new(UI, fileService, mondayConfig);
                return new MondaySpecflowService(UI, mondayClient, mondayConfig);
            }
            else if (serviceType == "Github")
            {
                GithubConfig githubConfig = new(config);
                return new GithubSpecflowService(UI, githubConfig);
            }

            throw new NotFoundException("The service type was entered incorrectly.");
        }

        public ISystemUserStoryService GetSystemUserStoryService()
        {
            var serviceType = config.GetValue<string>("serviceType");

            if (serviceType == "AzureDevOps")
            {
                AzureDevOpsConfig azureConfig = new(config);
                AzureDevOpsClient azureDevOpsClient = new(UI, fileService, azureConfig);
                return new AzureDevOpsUserStoryService(azureConfig, UI, fileService, azureDevOpsClient);
            }
            else if (serviceType == "Monday")
            {
                MondayConfig mondayConfig = new(config);
                MondayClient mondayClient = new(UI, fileService, mondayConfig);
                return new MondayUserStoryService(UI, fileService, mondayClient, mondayConfig);
            }
            else if (serviceType == "Github")
            {
                GithubConfig githubConfig = new(config);
                GithubClient githubClient = new (UI, fileService, githubConfig); 
                return new GithubUserStoryService(UI, fileService, githubConfig, githubClient);
            }

            throw new NotFoundException("The service type was entered incorrectly.");
        }
    }
}
