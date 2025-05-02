param (
    [string]$ConfigPath = ".appsettings.json",  # Path to config file
    [switch]$Debug  # Add debug switch for verbose logging
)

# Set error action preference
$ErrorActionPreference = "Continue"

$script:isWindowsOS = $PSVersionTable.PSVersion.Major -ge 5 -and $PSVersionTable.Platform -ne "Unix"
$pathSeparator = if ($script:isWindowsOS) { "\" } else { "/" }

if ($PSVersionTable.PSVersion.Major -lt 6) {
    # For Windows PowerShell 5.1 and below
    if (-not ('System.Runtime.InteropServices.RuntimeInformation' -as [type])) {
        Add-Type -AssemblyName System.Runtime.InteropServices.RuntimeInformation
    }
    # Additional platform check for older PowerShell
    if (-not $script:isWindowsOS) {
        $script:isWindowsOS = -not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Linux) -and 
                     -not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)
    }
}


# Helper function for logging
Function Write-Log {
    param ([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    # Only show DEBUG messages if Debug switch is enabled
    if ($Level -eq "DEBUG" -and -not $Debug) {
        return
    }
    
    Write-Host "[$timestamp] [$Level] $Message"
}

Write-Log "Script started with parameters: ConfigPath=$ConfigPath, Debug=$Debug"

# Resolve config path to absolute path
try {
    $ConfigPath = Resolve-Path $ConfigPath -ErrorAction Stop
    Write-Log "Resolved config path: $ConfigPath"
}
catch {
    Write-Log "Failed to resolve config path: $_" "ERROR"
    Write-Log "Working directory: $(Get-Location)" "DEBUG"
    exit 1
}

# Read configuration
if (-not (Test-Path $ConfigPath)) {
    Write-Log "Configuration file not found at: $ConfigPath" "ERROR"
    Write-Log "Working directory: $(Get-Location)" "DEBUG"
    exit 1
}

try {
    Write-Log "Reading configuration file..."
    $config = Get-Content -Path $ConfigPath -Raw | ConvertFrom-Json
    
    # Check if Path exists in config
    if ($null -eq $config.Path) {
        Write-Log "Path section not found in config" "ERROR"
        Write-Log "Config contents: $($config | ConvertTo-Json -Depth 1)" "DEBUG"
        exit 1
    }
    
    # Check if GitHub exists in config
    if ($null -eq $config.GitHub) {
        Write-Log "GitHub section not found in config" "ERROR"
        Write-Log "Config contents: $($config | ConvertTo-Json -Depth 1)" "DEBUG"
        exit 1
    }
    
    $Token = $config.GitHub.Token
    $Extension = $config.GitHub.Extension
    $Repositories = $config.GitHub.repositories
    $OutputDir = $config.Path.ShellDirectoryPath
    $GurkaFilesDir = [System.IO.Path]::Combine($(Split-Path -Parent $ConfigPath), "GurkaFiles")

    Write-Log "Config loaded successfully"
    Write-Log "Repositories: $($Repositories -join ', '), Extension: $Extension"
    Write-Log "OutputDir: $OutputDir"
    Write-Log "HelpersDir: $GurkaFilesDir"
}
catch {
    Write-Log "Failed to read configuration file: $_" "ERROR"
    exit 1
}

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    try {
        Write-Log "Creating output directory: $OutputDir"
        New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
        Write-Log "Created output directory: $OutputDir"
    }
    catch {
        Write-Log "Failed to create output directory '$OutputDir': $_" "ERROR"
        exit 1
    }
}

# Ensure helpers directory exists
if (-not (Test-Path $GurkaFilesDir)) {
    try {
        Write-Log "Creating helpers directory: $GurkaFilesDir"
        New-Item -Path $GurkaFilesDir -ItemType Directory -Force | Out-Null
        Write-Log "Created helpers directory: $GurkaFilesDir"
    }
    catch {
        Write-Log "Failed to create helpers directory '$GurkaFilesDir': $_" "ERROR"
        exit 1
    }
}

# Function to get the latest workflow run ID for a repository
function Get-LatestRunId {
    param (
        [string]$Repository,
        [string]$Token
    )
    
    Write-Log "Getting latest workflow run ID for $Repository"
    
    $headers = @{
        Authorization = "token $Token"
        Accept = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent" = "PowerShell-GitHub-CLI"
    }

    # Get the latest successful workflow run
    $runsUrl = "https://api.github.com/repos/$Repository/actions/runs?status=completed&per_page=1"
    Write-Log "API URL: $runsUrl" "DEBUG"
    
    try {
        $runsResponse = Invoke-RestMethod -Uri $runsUrl -Headers $headers -ErrorAction Stop
        
        if ($runsResponse.total_count -eq 0) {
            Write-Log "No completed workflow runs found" "ERROR"
            return $null
        }
        
        $latestRun = $runsResponse.workflow_runs[0]
        Write-Log "Latest run ID: $($latestRun.id), created at: $($latestRun.created_at)"
        return $latestRun.id
    }
    catch {
        Write-Log "Error getting latest run ID: $_" "ERROR"
        return $null
    }
}

# Function to get stored run ID for a repository
function Get-StoredRunId {
    param (
        [string]$Repository
    )
    
    # Extract just the repo name for the filename
    $repoName = $Repository.Split('/')[1]
    $repoFileName = "$($repoName)RunId.json"
    $runIdFilePath = [System.IO.Path]::Combine($GurkaFilesDir, $repoFileName)    
    Write-Log "Looking for run ID file: $runIdFilePath" "DEBUG"
    
    if (Test-Path $runIdFilePath) {
        try {
            $runIdData = Get-Content -Path $runIdFilePath -Raw | ConvertFrom-Json
            # Check for both possible property names
            $runId = if ($runIdData.RunId) { $runIdData.RunId } else { $runIdData."$($repoName)RunId" }
            Write-Log "Found stored run ID for $Repository`: $runId" "DEBUG"
            return $runId
        }
        catch {
            Write-Log "Error reading run ID file for $Repository`: $_" "ERROR"
            return 0
        }
    }
    else {
        Write-Log "No run ID file found for $Repository, creating default" "INFO"
        # Create default run ID file
        $defaultData = @{
            "RunId" = 0
        }
        $defaultData | ConvertTo-Json | Set-Content -Path $runIdFilePath
        return 0
    }
}

# Function to update RunId in repository-specific file
function Update-RunId {
    param (
        [string]$Repository,
        [long]$NewRunId
    )
    
    $repoName = $Repository.Split('/')[1]
    $repoFileName = "$($repoName)RunId.json"
    $runIdFilePath = [System.IO.Path]::Combine($GurkaFilesDir, $repoFileName)   

    try {
        $runIdData = @{
            "RunId" = $NewRunId
        }
        $runIdData | ConvertTo-Json | Set-Content -Path $runIdFilePath
        Write-Log "✅ Updated run ID for $Repository to: $NewRunId" "INFO"
        return $true
    }
    catch {
        Write-Log "Failed to update run ID file for $Repository`: $_" "ERROR"
        return $false
    }
}

# Function to fetch artifacts for a repository
function Get-GitHubArtifacts {
    param (
        [string]$Repository,
        [string]$Token,
        [long]$RunId,
        [string]$Extension,
        [string]$OutputDir
    )
    
    $headers = @{
        Authorization = "token $Token"
        Accept = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent" = "PowerShell-GitHub-CLI"
    }

    # Extract repo name for naming the output files
    $repoName = $Repository.Split('/')[1]
    Write-Log "Fetching artifacts for $Repository run ID: $RunId" "INFO"

    #Get all artifacts from a workflow run
    $artifactsUrl = "https://api.github.com/repos/$Repository/actions/runs/$RunId/artifacts"
    Write-Log "Fetching artifacts from URL: $artifactsUrl" "DEBUG"
    
    try {
        $artifactsResponse = Invoke-RestMethod -Uri $artifactsUrl -Headers $headers -ErrorAction Stop
        Write-Log "Found $($artifactsResponse.total_count) artifacts in total" "INFO"
    
        #Check for specific artifact "Gurka" first
        $gurkaArtifact = $artifactsResponse.artifacts | Where-Object { $_.name -eq "Gurka" }
        
        if ($gurkaArtifact) {
            Write-Log "Found specific 'Gurka' artifact" "INFO"
            $artifacts = @($gurkaArtifact)
        } else {
            # Fall back to checking extensions
            Write-Log "No specific 'Gurka' artifact found, searching for extensions matching: $Extension" "INFO"
            $artifacts = $artifactsResponse.artifacts | Where-Object { $_.name -like "*$Extension" }
        }
    
        if ($null -eq $artifacts -or $artifacts.Count -eq 0) {
            Write-Log "No matching artifacts found in run ID $RunId" "ERROR"
            return $null
        }
    
        Write-Log "Found $($artifacts.Count) matching artifacts. Downloading..." "INFO"
    
        #Download each matching artifact
        foreach ($artifact in $artifacts) {
            $downloadUrl = $artifact.archive_download_url
            $outputPath = [System.IO.Path]::Combine($OutputDir, "$($repoName)_$($artifact.name).zip")            
            # Skip if already downloaded
            if (Test-Path $outputPath) {
                Write-Log "Skipping: $($repoName)_$($artifact.name) (already exists)" "INFO"
                continue
            }

            Write-Log "Downloading: $($repoName)_$($artifact.name)..." "INFO"
            Invoke-RestMethod -Uri $downloadUrl -Headers $headers -OutFile $outputPath
            Write-Log "✅ Artifact downloaded to: $outputPath" "INFO"

            try {
                Write-Log "Extracting $outputPath directly to $OutputDir" "DEBUG"
                
                # Check if running on PowerShell Core where Expand-Archive works cross-platform
                if ($PSVersionTable.PSEdition -eq "Core") {
                    Expand-Archive -Path $outputPath -DestinationPath $OutputDir -Force
                }
                # Fallback for Linux if not using PowerShell Core
                elseif (-not $script:isWindowsOS) {
                    # Use unzip command on Linux
                    $unzipCommand = "unzip -o '$outputPath' -d '$OutputDir'"
                    Invoke-Expression $unzipCommand
                }
                # Windows PowerShell
                else {
                    Expand-Archive -Path $outputPath -DestinationPath $OutputDir -Force
                }
                
                Write-Log "✅ Artifact extracted directly to: $OutputDir" "INFO"
                
                # Delete the zip file after extraction
                Remove-Item -Path $outputPath -Force
                Write-Log "🗑️ Deleted zip file after extraction" "INFO"
            }
            catch {
                Write-Log "Failed to extract artifact: $_" "ERROR"
            }
        }
    
        Write-Log "✅ All $($artifacts.Count) artifacts for $Repository have been processed." "INFO"
        return $artifacts
    }
    catch {
        Write-Log "Error fetching artifacts for $Repository`: $_" "ERROR"
        return $null
    }
}

# Function to process artifacts for a specific repository
function Process-Repository {
    param (
        [string]$Repository,
        [string]$Token,
        [string]$Extension,
        [string]$OutputDir
    )
    
    Write-Log "Processing repository: $Repository" "INFO"
    
    # Get the stored run ID for this repository
    $StoredRunId = Get-StoredRunId -Repository $Repository
    
    # Get latest run ID
    $latestRunId = Get-LatestRunId -Repository $Repository -Token $Token
    
    if (-not $latestRunId) {
        Write-Log "Could not retrieve latest run ID for $Repository. Skipping." "ERROR"
        return $false
    }
    
    # Compare with stored run ID
    if ($latestRunId -eq $StoredRunId) {
        Write-Log "No new workflow runs detected for $Repository. Current run ID: $StoredRunId" "INFO"
        return $false
    }
    
    Write-Log "New workflow run detected for $Repository! Previous: $StoredRunId, Latest: $latestRunId" "INFO"
    
    # Download artifacts from the new run
    $artifacts = Get-GitHubArtifacts -Repository $Repository -Token $Token -RunId $latestRunId -Extension $Extension -OutputDir $OutputDir
    
    if ($artifacts) {
        # Update run ID file
        Update-RunId -Repository $Repository -NewRunId $latestRunId
        return $true
    }
    
    return $false
}

# Function to process all repositories
function Process-AllRepositories {
    $successCount = 0
    $totalCount = 0
    
    foreach ($repository in $Repositories) {
        $totalCount++
        Write-Log "Processing repository $totalCount of $($Repositories.Count): $repository" "INFO"
        $success = Process-Repository -Repository $repository -Token $Token -Extension $Extension -OutputDir $OutputDir
        if ($success) {
            $successCount++
        }
    }
    
    Write-Log "Processed $totalCount repositories. Successfully updated $successCount repositories." "INFO"
}

# Main execution block - simplified to just run once
Process-AllRepositories
Write-Log "Script execution completed" "INFO"