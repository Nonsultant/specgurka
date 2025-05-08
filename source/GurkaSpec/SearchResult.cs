namespace SpecGurka.GurkaSpec;

public class SearchResult
{
    public string DocType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public float Score { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public List<string> TypeSpecificTags { get; set; } = new List<string>();
        
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
        
    public string ParentFeatureId { get; set; } = string.Empty;
    public string ParentFeatureName { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
        
    public string BranchName { get; set; } = string.Empty;
    public string CommitId { get; set; } = string.Empty;
    public string CommitMessage { get; set; } = string.Empty;
    public string TestsPassed { get; set; } = string.Empty;
}