using SpecGurka.GherkinTools;
using SpecGurka.Interfaces;
using SpecGurka.Specflow;
using Microsoft.Extensions.Configuration;
using SpecGurka.Exceptions;

namespace SpecGurka;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        UIHelper UI = new();
        GherkinFileService fileService = new(UI, config);
        ServiceFactory serviceFactory = new(UI, fileService, config);

        int mode;

        if (args.Length != 0 && HasModeArgumentBeenPassedCorrectly(args[0]))
        {
            mode = int.Parse(args[0]);
        }
        else
        {
            mode = 0;
            UI.PrintTitle("Program started in 'DEFAULT MODE'");
        }


        GherkinFolderReader folderReader = new(UI);
        string[] gherkinFilesInFolder;

        try
        {
            gherkinFilesInFolder = folderReader.GetGherkinFilesInFolder(config.GetValue<string>("featureFilePath"));
        }
        catch
        {
            return;
        }

        ISystemClient systemClient;
        IWorkItemService workItemService;
        ISystemFileLinkService systemFileLinkService;
        ISystemSpecflowService systemSpecflowService;
        ISystemUserStoryService systemUserStoryService;

        try
        {
            systemClient = serviceFactory.GetServiceClient();
            systemFileLinkService = serviceFactory.GetSystemFileLinkService();
            systemSpecflowService = serviceFactory.GetSystemSpecflowService();
            systemUserStoryService = serviceFactory.GetSystemUserStoryService();
            workItemService = serviceFactory.GetWorkItemService();
        }
        catch
        {
            UI.PrintError("There was an error with retrieving the service. Make sure that the service type was entered correctly.");
            UI.PrintCancel();
            return;
        }

        GherkinFileReader gherkinReader = new(UI);

        List<Feature> features = new List<Feature>();

        foreach (var gherkinFilePath in gherkinFilesInFolder)
        {
            var fileName = Path.GetFileName(gherkinFilePath);
            UI.PrintReading("FILE", fileName);

            try
            {
                var feature = new Feature(fileService, systemClient, workItemService, systemFileLinkService,
                    systemSpecflowService, systemUserStoryService)
                { GherkinFilePath = gherkinFilePath };
                feature.GherkinFileContent = gherkinReader.ReadGherkinFile(gherkinFilePath);
                features.Add(feature);

                feature.VerifyGherkinFile();
                await feature.FetchFeatureItemFromService();
                feature.VerifyFeatureItemFromService();
            }
            catch
            {
                UI.PrintCancel();
                continue;
            }
        }

        //duplicated feature ids
        UI.PrintTitle("Testing For Files With Duplicated Ids.");

        try
        {
            fileService.TestForRepeatedFeatureTags(features);
        }
        catch
        {
            UI.PrintCancel("CANNOT CONTINUE WITH UPDATES.");
            return;
        }

        //duplicated user ids
        UI.PrintTitle("Testing For Files With Duplicated User Story Ids.");

        try
        {
            fileService.TestForRepeatedUserStoryTags(features);
        }
        catch
        {
        }

        //checking user stories against service
        UI.PrintTitle("Testing User Story Tags Against Service");
        await systemUserStoryService.TestIfUserStoriesExistOnSystem(features);

        //doesn't run updates if mode 1
        if (mode == 1)
            return;


        //specflow
        var specflowFilePath = config.GetValue<string>("specflow:specflowPath");

        SpecflowFileReader specflowReader = new();
        var specflowExecutionResult = specflowReader.ReadSpecflowFile(specflowFilePath);

        SpecflowFileService specflowService = new SpecflowFileService(UI);
        var specflowResults = specflowService.ExtractSpecflowFeatureResultsFromExecutionResults(specflowExecutionResult);

        foreach (var specflowResult in specflowResults)
        {
            foreach (var feature in features)
            {
                if (feature.ServiceFeatureItem != null)
                {
                    if (specflowResult.Title == feature.ServiceFeatureItem.Title)
                    {
                        feature.SpecflowResult = specflowResult;
                    }
                }
            }
        }

        UI.PrintTitle("Updating Service With Specflow Results");

        foreach (var feature in features)
        {
            if (feature.SpecflowResult != null)
            {
                if (!feature.IsSpecFlowStatusUpToDate())
                {
                    await feature.UpdateServiceItemWithSpecflowResult();
                }
            }
        }

        UI.PrintTitle("Updating File Links On Service.");

        foreach (var feature in features)
        {
            var correctFileLink = fileService.CreateGherkinFileLink(feature.GherkinFilePath);

            if (feature.ServiceFeatureItem != null)
            {
                var cleanFileLinkFromService = systemFileLinkService.CleanSystemFileLink(feature.GherkinFilePath,
                    feature.ServiceFeatureItem.FileLink);

                if (cleanFileLinkFromService != correctFileLink)
                {
                    await systemFileLinkService.UpdateSystemFileLink(correctFileLink, feature);
                }
            }
        }


    }

    private static bool HasModeArgumentBeenPassedCorrectly(string passedMode, UIHelper UI)
    {
        string[] availableModes = { "1" };

        if (availableModes.Contains(passedMode))
        {
            UI.PrintTitle($"Program starts in 'MODE {passedMode}'");
            return true;
        }

        UI.PrintError("Invalid Argument to start program. Please choose another Argument.");
        throw new InvalidArgumentException();
    }
}
