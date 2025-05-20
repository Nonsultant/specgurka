using System.Text.Json.Serialization;

namespace SpecGurka.GurkaSpec;

public class GitHubFetcher
{
    public class Config
    {
        public PathConfig Path { get; set; }
        public GitHubConfig GitHub { get; set; }
    }

    public class PathConfig
    {
        public string directoryPath { get; set; }
    }

    public class GitHubConfig
    {
        public string Token { get; set; }
        public List<string> Repositories { get; set; }
    }

    public class RunIdData
    {
        public long RunId { get; set; }
    }

    public class WorkflowRunsResponse
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
    
        [JsonPropertyName("workflow_runs")]
        public List<WorkflowRun> WorkflowRuns { get; set; }
    }

    public class WorkflowRun
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class ArtifactsResponse
    {
        public List<Artifact> Artifacts { get; set; }
    }

    public class Artifact
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    
        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }
    
        [JsonPropertyName("name")]
        public string Name { get; set; }
    
        [JsonPropertyName("size_in_bytes")]
        public long SizeInBytes { get; set; }
    
        [JsonPropertyName("url")]
        public string Url { get; set; }
    
        [JsonPropertyName("archive_download_url")]
        public string ArchiveDownloadUrl { get; set; }
    
        [JsonPropertyName("expired")]
        public bool Expired { get; set; }
    
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}