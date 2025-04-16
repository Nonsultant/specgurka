namespace VizGurka.Helpers;

public class QueryMapperHelper
{
    private readonly Dictionary<string, string> _fieldPrefixMappings;

    public QueryMapperHelper()
    {
        _fieldPrefixMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "feature:", "FeatureName:" },
            { "egenskap:", "FeatureName:" },
            { "feature name:", "FeatureName:" },
            { "feature id:", "FeatureId:" },
            { "feature description:", "FeatureDescription:" },
            { "feature status:", "FeatureStatus:" },

            { "scenario:", "ScenarioName:" },
            { "test:", "ScenarioName:" },
            { "scenario name:", "ScenarioName:" },
            { "scenario status:", "ScenarioStatus:" },
            { "scenario duration:", "ScenarioTestDuration:" },

            { "step:", "StepText:" },
            { "steg:", "StepText:" },

            { "tag:", "Tag:" },
            { "tags:", "Tag:" },
            { "@", "Tag:" },
            { "featuretag:", "FeatureTag:" },
            { "feature tag:", "FeatureTag:" },
            { "scenariotag:", "ScenarioTag:" },
            { "scenario tag:", "ScenarioTag:" },
            { "rule:", "RuleName:" },
            { "rule description:", "RuleDescription:" },
            { "rule status:", "RuleStatus:" },
            { "ruletag:", "RuleTag:" },
            { "rule tag:", "RuleTag:" },

            { "file:", "FileName:" },
            { "fil:", "FileName:" },
            { "name:", "Name:" },
            { "namn:", "Name:" },
            { "branch:", "BranchName:" },
            { "commit:", "CommitId:" },
            { "message:", "CommitMessage:" },

            { "parent:", "ParentFeatureId:" },
            { "parent feature:", "ParentFeatureId:" },
            { "parent name:", "ParentFeatureName:" },
            { "StepText:", "StepText:"}
        };
    }

    public string MapQuery(string query)
    {
        foreach (var mapping in _fieldPrefixMappings)
            if (query.StartsWith(mapping.Key, StringComparison.OrdinalIgnoreCase))
                return mapping.Value + query.Substring(mapping.Key.Length);

        return query;
    }

    public Dictionary<string, string> GetMappings() => _fieldPrefixMappings;
}