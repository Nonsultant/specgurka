namespace VizGurka.Models
{
    public class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float Score { get; set; }
        public string SourceField { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string StepText { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();
        public List<string> TypeSpecificTags { get; set; } = new();

        public string FeatureId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string ParentFeatureId { get; set; } = string.Empty;
        public string ParentFeatureName { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
    }
}