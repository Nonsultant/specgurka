param (
    [string]$Owner,
    [string]$Repo,
    [string]$ArtifactName,
    [string]$Token,
    [int]$RunId
)

$headers = @{
    Authorization = "token $Token"
    Accept        = "application/vnd.github+json"
    "User-Agent"  = "PowerShell-GitHub-CLI"
}

# Step 1: Get all artifacts from a workflow run
$artifactsUrl = "https://api.github.com/repos/$Owner/$Repo/actions/runs/$RunId/artifacts"
$artifactsResponse = Invoke-RestMethod -Uri $artifactsUrl -Headers $headers

# Step 2: Find the specific artifact by name
$artifact = $artifactsResponse.artifacts | Where-Object { $_.name -eq $ArtifactName }

if (-not $artifact) {
    Write-Error "Artifact '$ArtifactName' not found in run ID $RunId"
    exit 1
}

# Step 3: Download the artifact
$downloadUrl = $artifact.archive_download_url
$outputPath = "$ArtifactName.zip"
Invoke-RestMethod -Uri $downloadUrl -Headers $headers -OutFile $outputPath

Write-Host "✅ Artifact downloaded to: $outputPath"
