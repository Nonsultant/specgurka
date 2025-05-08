using System.Text.Json;
using SpecGurka.GurkaSpec;
using GurkaSpec;

namespace VizGurka.Services;
public class GitHubActionFetcher
{
    
    private readonly bool _debug;
    private readonly HttpClient _httpClient;
    private GitHubFetcher.Config _config;

    public GitHubActionFetcher(IConfiguration configuration, bool debug = false)
    {
        _config = new GitHubFetcher.Config();
        _config.GitHub = configuration.GetSection("GitHub").Get<GitHubFetcher.GitHubConfig>();
        _config.Path = configuration.GetSection("Path").Get<GitHubFetcher.PathConfig>();
        _debug = debug;
        _httpClient = new HttpClient();
    }

    public async Task RunAsync()
    {
        Log("Script started");

        
        if (!Directory.Exists(_config.Path.directoryPath))
        {
            Log($"Creating directory: {_config.Path.directoryPath}");
            Directory.CreateDirectory(_config.Path.directoryPath);
        }

        foreach (var repository in _config.GitHub.Repositories)
        {
            Log($"Processing repository: {repository}");
            var success = await ProcessRepositoryAsync(repository);
            if (success)
            {
                Log($"Successfully processed repository: {repository}");
            }
            else
            {
                Log($"Skipped processing repository: {repository}");
            }
        }

        Log("Script execution completed");
    }



    private async Task<bool> ProcessRepositoryAsync(string repository)
    {
        var storedRunId = GetStoredRunId(repository);
        var latestRunId = await GetLatestRunIdAsync(repository);

        if (latestRunId == null)
        {
            Log($"Could not retrieve latest run ID for {repository}. Skipping.", "ERROR");
            return false;
        }

        if (storedRunId == latestRunId)
        {
            Log($"No new workflow runs detected for {repository}. Current run ID: {storedRunId}");
            return false;
        }

        Log($"New workflow run detected for {repository}! Previous: {storedRunId}, Latest: {latestRunId}");

        var artifacts = await GetGitHubArtifactsAsync(repository, latestRunId.Value);
    
        // Update the run ID even if no matching artifacts were found
        UpdateRunId(repository, latestRunId.Value);
    
        return artifacts != null && artifacts.Count > 0;
    }

    private long? GetStoredRunId(string repository)
    {
        try
        {
            var repoName = GetRepoName(repository);
            var runIdFilePath = Path.Combine(_config.Path.directoryPath, $"{repoName}RunId.json");

            if (File.Exists(runIdFilePath))
            {
                var runIdData = JsonSerializer.Deserialize<GitHubFetcher.RunIdData>(File.ReadAllText(runIdFilePath));
                return runIdData?.RunId;
            }

            Log($"No run ID file found for {repository}. Creating default.");
            var defaultRunId = new GitHubFetcher.RunIdData { RunId = 0 };
            File.WriteAllText(runIdFilePath, JsonSerializer.Serialize(defaultRunId));
            return 0;
        }
        catch (Exception ex)
        {
            Log($"Error reading run ID file for {repository}: {ex.Message}", "ERROR");
            return 0;
        }
    }

    private async Task<long?> GetLatestRunIdAsync(string repository)
{
    try
    {
        Log($"Getting latest workflow run ID for {repository}");

        // Construct the API URL
        var url = $"https://api.github.com/repos/{repository}/actions/runs?status=completed&per_page=1";
        Log($"API URL: {url}", "DEBUG");

        // Create HTTP request
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"token {_config.GitHub.Token}");
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Add("User-Agent", "CSharp-GitHub-CLI");

        // Log all request headers for debugging
        foreach (var header in request.Headers)
        {
            Log($"Request Header: {header.Key}={string.Join(",", header.Value)}", "DEBUG");
        }

        // Send the request
        var response = await _httpClient.SendAsync(request);

        // Log the status code
        Log($"Response Status Code: {(int)response.StatusCode} - {response.StatusCode}", "DEBUG");
        
        var content = await response.Content.ReadAsStringAsync();
        Log($"Response Content: {content}", "DEBUG");
        
        response.EnsureSuccessStatusCode();
        
        try
        {
            using JsonDocument doc = JsonDocument.Parse(content);
            JsonElement root = doc.RootElement;
            
            if (root.TryGetProperty("total_count", out JsonElement totalCountElement))
            {
                int totalCount = totalCountElement.GetInt32();
                Log($"JSON total_count: {totalCount}", "DEBUG");
                
                if (totalCount > 0 && root.TryGetProperty("workflow_runs", out JsonElement workflowRunsElement) && workflowRunsElement.ValueKind == JsonValueKind.Array)
                {
                    if (workflowRunsElement.GetArrayLength() > 0)
                    {
                        JsonElement firstRun = workflowRunsElement[0];
                        
                        if (firstRun.TryGetProperty("id", out JsonElement idElement) && 
                            firstRun.TryGetProperty("created_at", out JsonElement createdAtElement))
                        {
                            long id = idElement.GetInt64();
                            string createdAt = createdAtElement.GetString();
                            
                            Log($"Found workflow run: ID={id}, Created At={createdAt}", "DEBUG");
                            
                            return id;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error parsing JSON manually: {ex.Message}", "ERROR");
        }
        
        Log("Attempting standard deserialization...", "DEBUG");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var runsResponse = JsonSerializer.Deserialize<GitHubFetcher.WorkflowRunsResponse>(content, options);

        if (runsResponse != null && runsResponse.TotalCount > 0 && runsResponse.WorkflowRuns?.Any() == true)
        {
            var latestRun = runsResponse.WorkflowRuns.First();
            Log($"Latest run ID: {latestRun.Id}, created at: {latestRun.CreatedAt}");
            
            return latestRun.Id;
        }

        Log("No completed workflow runs found", "ERROR");
        
        return null;
    }
    catch (Exception ex)
    {
        Log($"Error getting latest run ID: {ex.Message}", "ERROR");
        return null;
    }
}

   private async Task<List<GitHubFetcher.Artifact>> GetGitHubArtifactsAsync(string repository, long runId)
{
    try
    {
        Log($"Fetching artifacts for {repository} run ID: {runId}");
        var url = $"https://api.github.com/repos/{repository}/actions/runs/{runId}/artifacts";
        Log($"API URL: {url}", "DEBUG");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"token {_config.GitHub.Token}");
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Add("User-Agent", "CSharp-GitHub-CLI");

        var response = await _httpClient.SendAsync(request);
        
        var content = await response.Content.ReadAsStringAsync();
        Log($"Response Content: {content}", "DEBUG");

        response.EnsureSuccessStatusCode();
        
        var options = new JsonSerializerOptions { 
            PropertyNameCaseInsensitive = true 
        };
        
        var artifactsResponse = JsonSerializer.Deserialize<GitHubFetcher.ArtifactsResponse>(content, options);

        if (artifactsResponse?.Artifacts == null)
        {
            Log("Artifacts collection is null after deserialization", "ERROR");
            return new List<GitHubFetcher.Artifact>();
        }
        
        Log($"Found {artifactsResponse.Artifacts.Count} artifacts in total");
        
        var matchingArtifacts = artifactsResponse.Artifacts
            .Where(a => a != null && a.Name != null && a.Name.Contains("gurka", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Log($"Found {matchingArtifacts.Count} matching artifacts containing 'gurka' in the name");

        if (matchingArtifacts.Count == 0)
        {
            Log($"No matching artifacts found in run ID {runId}", "ERROR");
            return null; 
        }
        
        foreach (var artifact in matchingArtifacts)
        {
            Log($"Artifact details - ID: {artifact.Id}, Name: {artifact.Name}, Size: {artifact.SizeInBytes} bytes");
            Log($"Download URL present: {!string.IsNullOrEmpty(artifact.ArchiveDownloadUrl)}");
            
            if (string.IsNullOrEmpty(artifact.ArchiveDownloadUrl))
            {
                Log("WARNING: Download URL is missing for this artifact!", "ERROR");
            }
            else
            {
                Log($"Download URL starts with: {artifact.ArchiveDownloadUrl.Substring(0, Math.Min(30, artifact.ArchiveDownloadUrl.Length))}...", "DEBUG");
            }
        }

        List<GitHubFetcher.Artifact> successfullyProcessed = new List<GitHubFetcher.Artifact>();
        foreach (var artifact in matchingArtifacts)
        {
            bool success = await DownloadAndExtractArtifactAsync(repository, artifact);
            if (success)
            {
                successfullyProcessed.Add(artifact);
            }
        }

        return successfullyProcessed.Count > 0 ? successfullyProcessed : null;
    }
    catch (Exception ex)
    {
        Log($"Error fetching artifacts for {repository}: {ex.Message}", "ERROR");
        return null;
    }
}
    private async Task<bool> DownloadAndExtractArtifactAsync(string repository, GitHubFetcher.Artifact artifact)
{
    if (artifact == null || string.IsNullOrEmpty(artifact.Name) || string.IsNullOrEmpty(artifact.ArchiveDownloadUrl))
    {
        Log("Invalid artifact or download URL", "ERROR");
        return false;
    }

    try
    {
        var repoName = GetRepoName(repository);
        var zipFileName = $"{repoName}_{artifact.Name}.zip";
        var outputPath = Path.Combine(_config.Path.directoryPath, zipFileName);
        
        Log($"Downloading artifact: {artifact.Name}");

        var request = new HttpRequestMessage(HttpMethod.Get, artifact.ArchiveDownloadUrl);
        request.Headers.Add("Authorization", $"token {_config.GitHub.Token}");
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Add("User-Agent", "CSharp-GitHub-CLI");

        using (var response = await _httpClient.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();

            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }
        }
        
        Log($"Artifact downloaded to: {outputPath}");
        
        var extractPath = Path.Combine(_config.Path.directoryPath);
        
        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
            Log($"Created extraction directory: {extractPath}");
        }
        
        Log($"Extracting ZIP file to: {extractPath}");
        
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(outputPath, extractPath, true);
            Log($"Successfully extracted artifact contents to: {extractPath}");
            
            File.Delete(outputPath);
            Log($"Deleted ZIP file: {zipFileName}");
            
            return true;
        }
        catch (Exception ex)
        {
            Log($"Error extracting ZIP file: {ex.Message}", "ERROR");
            return false;
        }
    }
    catch (Exception ex)
    {
        Log($"Failed to download/extract artifact: {ex.Message}", "ERROR");
        return false;
    }
}
    private void UpdateRunId(string repository, long newRunId)
    {
        try
        {
            var repoName = GetRepoName(repository);
            var runIdFilePath = Path.Combine(_config.Path.directoryPath, $"{repoName}RunId.json");

            var runIdData = new GitHubFetcher.RunIdData { RunId = newRunId };
            File.WriteAllText(runIdFilePath, JsonSerializer.Serialize(runIdData));
            Log($"Updated run ID for {repository} to: {newRunId}");
        }
        catch (Exception ex)
        {
            Log($"Failed to update run ID file for {repository}: {ex.Message}", "ERROR");
        }
    }

    private string GetRepoName(string repository) => repository.Split('/')[1];

    private void Log(string message, string level = "INFO")
    {
        if (level == "DEBUG" && !_debug) return;
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
    }
    
}