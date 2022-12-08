using SpecGurka.Exceptions; 
using Gherkin.Ast;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace SpecGurka.GherkinTools;

public class GherkinFileService
{
    private readonly UIHelper UI;
    private readonly IConfiguration config;

    public GherkinFileService(UIHelper UI, IConfiguration config)
    {
        this.UI = UI;
        this.config = config;
    }

    public List<string> GetAllFeatureIdsFromADoc(List<GherkinDocument> gherkinDocs)
    {
        List<string> featureIds = new List<string>();

        foreach (var gherkinDoc in gherkinDocs)
        {
            featureIds.Add(GetFeatureId(gherkinDoc));
        }
        return featureIds;
    }

    public void VerifyGherkinId(GherkinDocument gherkinDoc)
    {
        var featureIds = GetAllFeatureIdsFromTags(gherkinDoc);

        TestFeatureId(featureIds);
        return;
    }

    public string GetFeatureId(GherkinDocument gherkinDoc)
    {
        var featureIds = GetAllFeatureIdsFromTags(gherkinDoc);
        return featureIds[0];
    }

    public void TestFeatureId(List<string> featureIds)
    {
        if (featureIds.Count > 1)
        {
            UI.PrintError("More than one feature id on feature level.");
            throw new TooManyFeatureIdsException("More than one feature id on feature level");
        }

        if (featureIds.Count == 0)
        {
            UI.PrintError("Missing feature id on feature level.");
            throw new NotFoundException("No feature id on feature level");
        }

        if (featureIds[0] == "0")
        {
            UI.PrintError("The feature id is 0.");
            throw new ArgumentException("The feature id is 0");
        }

        UI.PrintOk($"The feature id '{featureIds[0]}' on feature level");
    }

    private List<string> GetAllFeatureIdsFromTags(GherkinDocument gherkinDoc)
    {
        string pattern = @"Feature-(\d+)";
        List<string> featureIds = new List<string>();

        foreach (var tag in gherkinDoc.Feature.Tags)
        {
            Match match = Regex.Match(tag.Name, pattern, RegexOptions.IgnoreCase);
            string featureId;

            if (match.Success)
            {
                featureId = match.Groups[1].Value;
                featureIds.Add(featureId);
            }
        }

        return featureIds;
    }

    public void CheckIfFeatureIdsOnScenarioLevel(GherkinDocument gherkinDoc)
    {
        string pattern = @"Feature-(\d+)";
        bool hasNoFeatureIdsInScenario = true;

        foreach (IHasTags scenario in gherkinDoc.Feature.Children)
        {
            foreach (var tag in scenario.Tags)
            {
                Match match = Regex.Match(tag.Name, pattern, RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    UI.PrintWarning("Feature on scenario level.");
                    hasNoFeatureIdsInScenario = false;
                }
            }
        }

        if (hasNoFeatureIdsInScenario)
            UI.PrintOk("No feature on scenario level.");
    }

    public void VerifyGherkinFeatureTitle(GherkinDocument gherkinDoc)
    {
        string title;

        try
        {
            title = GetFeatureTitle(gherkinDoc);
        }
        catch (NotFoundException ex)
        {
            // Error has already been printed in GetFeatureTitle()
            throw;
        }
        catch 
        {
            UI.PrintError("There was an error when retrieving the title of the feature from the file.");
            throw;
        }

        UI.PrintOk($"The feature file has a title.");
    }

    private string GetFeatureTitle(GherkinDocument gherkinDoc)
    {
        string title = gherkinDoc.Feature.Name;

        if (title == null || title == "")
        {
            UI.PrintError("The feature file doesn't have a valid name.");
            throw new NotFoundException("The feature doesn't have a valid title.");
        }
        return title;
    }

    public void VerifyGherkinFileNameAndTitle(GherkinDocument gherkinDoc, string gherkinFilePath)
    {
        var title = GetCleanFeatureTitle(gherkinDoc);
        var fileName = GetCleanFeatureFileName(gherkinFilePath);

        if (title == fileName)
        {
            UI.PrintOk("The feature's title and file name match.");
            return;
        }

        UI.PrintWarning("The feature's title and file name do not match.");
    }

    private string GetCleanFeatureTitle(GherkinDocument gherkinDoc)
    {
        string rawTitle = GetFeatureTitle(gherkinDoc);
        string title = CleanFileNameOrTitle(rawTitle);

        return title;
    }

    private string GetCleanFeatureFileName(string gherkinFilePath)
    {
        string rawFileName = Path.GetFileName(gherkinFilePath);
        string fileName = CleanFileNameOrTitle(rawFileName);

        return fileName;
    }

    private string CleanFileNameOrTitle(string rawName)
    {
        string pattern = @" |,|'|-|_|´|.feature";
        string substitution = @"";
        RegexOptions options = RegexOptions.Multiline;

        Regex regex = new Regex(pattern, options);
        string result = regex.Replace(rawName, substitution).ToLower();

        return result;
    }

    public void TestForRepeatedFeatureTags(List<Feature> features)
    {
        var gherkinFilesWithDuplicatedIds = GetAllFilesWithDuplicatedFeatureIds(features);

        if (gherkinFilesWithDuplicatedIds.Count == 0)
            return;

        foreach (var gherkinFile in gherkinFilesWithDuplicatedIds)
        {
            UI.PrintError($"{gherkinFile.fileName} has a duplicated id ({gherkinFile.id}).");
        }

        throw new WrongFeatureConfigurationException("There are duplicated ids in the gherkin files.");
    }

    private List<dynamic> GetGherkinFileContent(List<Feature> features)
    {
        List<dynamic> gherkinFiles = new List<dynamic>();

        foreach (var feature in features)
        {
            string featureId;

            try
            {
                //feature id
                featureId = GetFeatureId(feature.GherkinFileContent);
            }
            catch
            {
                continue;
                //if the file doesn't have a feature id
            }

            var fileName = Path.GetFileName(feature.GherkinFilePath);

            gherkinFiles.Add(new { id = featureId, fileName = fileName });
        }

        return gherkinFiles;
    }

    private List<dynamic> GetAllFilesWithDuplicatedFeatureIds(List<Feature> features)
    {
        var gherkinFiles = GetGherkinFileContent(features);
        var queries = QueryForAllDuplicatedFeatureIds(gherkinFiles);

        List<dynamic> filesWithDuplicatedIds = new();

        foreach (var gherkinFile in gherkinFiles)
        {
            foreach (var query in queries)
            {
                if (query == gherkinFile.id)
                    filesWithDuplicatedIds.Add(gherkinFile);
            }
        }

        return filesWithDuplicatedIds;
    }

    private List<dynamic> QueryForAllDuplicatedFeatureIds(List<dynamic> gherkinFiles)
    {
        var queries = gherkinFiles.GroupBy(x => x.id)
            .Where(g => g.Count() > 1)
            .Select(y => y.Key)
            .ToList();

        return queries;
    }

    private List<string> ExtractUserStoryIdsFromTags(IEnumerable<Tag> tags)
    {
        string pattern = @"us-(\d+)";
        List<string> userStoryIds = new List<string>();

        foreach (var tag in tags)
        {
            Match match = Regex.Match(tag.Name, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var userStoryId = match.Groups[1].Value;
                userStoryIds.Add(userStoryId);
            }
        }

        return userStoryIds;

    }

    private List<List<string>> GetGroupedUserStoryIdsFromScenarioLevel(GherkinDocument gherkinFileContent)
    {
        List<List<string>> idsGroupedByScenario = new();

        foreach (IHasTags scenario in gherkinFileContent.Feature.Children)
        {
            var ids = ExtractUserStoryIdsFromTags(scenario.Tags);
            idsGroupedByScenario.Add(ids);
        }

        return idsGroupedByScenario;
    }

    public List<string> GetAllUserStoryIdsOnScenarioLevel(GherkinDocument gherkinFileContent)
    {
        List<string> allIds = new();

        foreach (IHasTags scenario in gherkinFileContent.Feature.Children)
        {
            var ids = ExtractUserStoryIdsFromTags(scenario.Tags);
            allIds.AddRange(ids);
        }

        return allIds;
    }

    public void TestForRepeatedUserStoryTags(List<Feature> features)
    {
        bool duplicatedIdsExist = false;
        
        foreach (var feature in features)
        {
            try
            {
                TestForRepeatedUserStoryTagsWithinScenarioLevel(feature);
                TestForRepeatedUserStoryTagsWithinFeatureLevel(feature);
                TestForRepeatedUserStoryTagsBetweenScenarioLevelAndFeatureLevel(feature);
            }
            catch
            {
                duplicatedIdsExist = true;
                continue; //if there are duplications within a scenario level, we won't check between scenario and feature
            }
        }

        if (duplicatedIdsExist)
            throw new WrongFeatureConfigurationException("There are duplicated userstory tags.");
    }

    private void TestForRepeatedUserStoryTagsWithinScenarioLevel(Feature feature)
    {
        var duplicatedIds = GetDuplicatedUserStoryIdsFromScenarioLevel(feature.GherkinFileContent);

        var fileName = Path.GetFileName(feature.GherkinFilePath);
        
        foreach(var duplicatedId in duplicatedIds)
        {
            UI.PrintError($"User story '{duplicatedId}' is repeated on scenario level within file '{fileName}'.");
        }

        if (duplicatedIds.Count > 0)
            throw new WrongFeatureConfigurationException("Duplicated tags on scenario level");

    }

    private List<string> GetDuplicatedUserStoryIdsFromScenarioLevel(GherkinDocument gherkinFileContent)
    {
        var idsGroupedByUserStory = GetGroupedUserStoryIdsFromScenarioLevel(gherkinFileContent);
        var duplicatedIds = QueryForDuplicatedUserStoryIds(idsGroupedByUserStory);

        return duplicatedIds;
    }

    private List<string> QueryForDuplicatedUserStoryIds(List<List<string>> idsGroupedByUserStory)
    {
        List<string> rawDuplicatedIds = new();

        foreach(var ids in idsGroupedByUserStory)
        {
            var q = ids.GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(y => y.Key)
            .ToList();

            rawDuplicatedIds.AddRange(q);
        }

        var duplicatedIds = rawDuplicatedIds.Select(x=>x).Distinct().ToList();

        return duplicatedIds;
    }

    private List<string> QueryForDuplicatedUserStoryIds(List<string> ids)
    {
        var duplicatedIds = ids.GroupBy(x => x)
        .Where(g => g.Count() > 1)
        .Select(y => y.Key)
        .ToList();

        return duplicatedIds;
    }

    private void TestForRepeatedUserStoryTagsBetweenScenarioLevelAndFeatureLevel(Feature feature)
    {
        var featureLevelUserStoryIds = GetAllUserStoryIdsOnFeatureLevel(feature.GherkinFileContent);
        var rawScenarioLevelUserStoryIds = GetAllUserStoryIdsOnScenarioLevel(feature.GherkinFileContent);
        var scenarioLevelUserStoryIds = RemoveDuplicatedIds(rawScenarioLevelUserStoryIds);
        var fileName = Path.GetFileName(feature.GherkinFilePath);
        
        foreach(var featureLevelId in featureLevelUserStoryIds)
        {
            foreach(var scenarioLevelId in scenarioLevelUserStoryIds)
            {
                if(featureLevelId == scenarioLevelId)
                {
                    UI.PrintError($"User story '{featureLevelId}' is present on both feature level and scenario level in file '{fileName}'");
                }

            }
        }
    }

    public List<string> GetAllUserStoryIdsOnFeatureLevel(GherkinDocument gherkinFileContent)
    {
        List<string> allIds = ExtractUserStoryIdsFromTags(gherkinFileContent.Feature.Tags);

        return allIds;
    }

    public List<string> RemoveDuplicatedIds(List<string> idsWithDuplicates)
    {
        var ids = idsWithDuplicates.GroupBy(x => x)
           .Select(y => y.Key)
           .ToList();

        return ids;
    }

    private void TestForRepeatedUserStoryTagsWithinFeatureLevel(Feature feature)
    {
        var ids = GetAllUserStoryIdsOnFeatureLevel(feature.GherkinFileContent);
        var duplicatedIds = QueryForDuplicatedUserStoryIds(ids);
        var fileName = Path.GetFileName(feature.GherkinFilePath);

        if(duplicatedIds.Count > 0)
        {
            foreach (string id in duplicatedIds)
            {
                UI.PrintError($"User story '{id}' is duplicated on feature level in file '{fileName}'.");
            }

            throw new WrongFeatureConfigurationException("There are duplicated ids on scenario level.");

        }
    }
    
    public string CreateGherkinFileLink(string filePath)
    {
        var featureRepoUrl = config.GetValue<string>("featureRepoUrl");
        var featureFolder = config.GetValue<string>("featureFilePath");
        var cleanFilePath = filePath.Replace(featureFolder, "").Replace("\\", "/");

        return featureRepoUrl + cleanFilePath;
    }

}
