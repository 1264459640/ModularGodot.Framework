# ModularGodot Framework Project Installation Script
# Used to add all csproj files from Core directory to solution

param(
    [string]$SolutionPath = "",
    [string]$CorePath = "",
    [switch]$Force = $false
)

# Set error handling
$ErrorActionPreference = "Stop"

# Color output function
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Generate new GUID
function New-ProjectGuid {
    return [System.Guid]::NewGuid().ToString().ToUpper()
}

# Find solution file
function Find-SolutionFile {
    param([string]$StartPath)
    
    $currentPath = $StartPath
    while ($currentPath -and (Test-Path $currentPath)) {
        $slnFiles = Get-ChildItem -Path $currentPath -Filter "*.sln" -File
        if ($slnFiles.Count -gt 0) {
            return $slnFiles[0].FullName
        }
        $parentPath = Split-Path $currentPath -Parent
        if ($parentPath -eq $currentPath) {
            break
        }
        $currentPath = $parentPath
    }
    return $null
}

# Get project information
function Get-ProjectInfo {
    param([string]$ProjectPath)
    
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $solutionDir = Split-Path $script:SolutionPath
    
    # Calculate relative path manually for compatibility
    $relativePath = $ProjectPath.Replace($solutionDir + [System.IO.Path]::DirectorySeparatorChar, "")
    
    return @{
        Name = $projectName
        Path = $ProjectPath
        RelativePath = $relativePath
        Guid = New-ProjectGuid
    }
}

# Parse solution folder structure
function Get-SolutionFolders {
    param([string]$SolutionContent)
    
    $folders = @{}
    $lines = $SolutionContent -split "`n"
    
    foreach ($line in $lines) {
        if ($line -match 'Project\("{2150E333-8FDC-42A3-9474-1A3956D46DE8}"\) = "([^"]+)", "([^"]+)", "{([^}]+)}"') {
            $folderName = $matches[1]
            $folderGuid = $matches[3]
            $folders[$folderName] = $folderGuid
        }
    }
    
    return $folders
}

# Determine project folder
function Get-ProjectFolder {
    param([string]$ProjectPath)
    
    $pathParts = $ProjectPath.Split([System.IO.Path]::DirectorySeparatorChar)
    $coreIndex = -1
    
    for ($i = 0; $i -lt $pathParts.Length; $i++) {
        if ($pathParts[$i] -eq "Core") {
            $coreIndex = $i
            break
        }
    }
    
    if ($coreIndex -ge 0 -and $coreIndex + 1 -lt $pathParts.Length) {
        return $pathParts[$coreIndex + 1]
    }
    
    return "0_Base"
}

# Check if project already exists in solution
function Test-ProjectExists {
    param(
        [string]$SolutionContent,
        [string]$ProjectName
    )
    
    return $SolutionContent -match "Project.*= `"$ProjectName`""
}

# Add projects to solution
function Add-ProjectToSolution {
    param(
        [string]$SolutionPath,
        [array]$Projects
    )
    
    $solutionContent = Get-Content $SolutionPath -Raw
    $folders = Get-SolutionFolders $solutionContent
    
    # New folders to create
    $newFolders = @{}
    
    # Check and create missing folders
    foreach ($project in $Projects) {
        $folderName = Get-ProjectFolder $project.Path
        if (-not $folders.ContainsKey($folderName) -and -not $newFolders.ContainsKey($folderName)) {
            $newFolders[$folderName] = New-ProjectGuid
        }
    }
    
    # Add new folders to solution
    $insertPoint = $solutionContent.IndexOf("Global")
    $beforeGlobal = $solutionContent.Substring(0, $insertPoint)
    $afterGlobal = $solutionContent.Substring($insertPoint)
    
    $newContent = $beforeGlobal
    
    # Add new folder definitions
    foreach ($folderName in $newFolders.Keys) {
        $folderGuid = $newFolders[$folderName]
        $newContent += "Project(`"{2150E333-8FDC-42A3-9474-1A3956D46DE8}`") = `"$folderName`", `"$folderName`", `"{$folderGuid}`"`r`n"
        $newContent += "EndProject`r`n"
        $folders[$folderName] = $folderGuid
    }
    
    # Add project definitions
    foreach ($project in $Projects) {
        if (-not (Test-ProjectExists $solutionContent $project.Name)) {
            $newContent += "Project(`"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`") = `"$($project.Name)`", `"$($project.RelativePath)`", `"{$($project.Guid)}`"`r`n"
            $newContent += "EndProject`r`n"
            Write-ColorOutput "  Added project: $($project.Name)" "Green"
        } else {
            Write-ColorOutput "  Project already exists: $($project.Name)" "Yellow"
        }
    }
    
    $newContent += $afterGlobal
    
    # Add project configurations
    $configSection = "GlobalSection(ProjectConfigurationPlatforms) = postSolution"
    $configIndex = $newContent.IndexOf($configSection)
    if ($configIndex -ge 0) {
        $configEndIndex = $newContent.IndexOf("EndGlobalSection", $configIndex)
        $beforeConfig = $newContent.Substring(0, $configEndIndex)
        $afterConfig = $newContent.Substring($configEndIndex)
        
        $configContent = $beforeConfig
        
        foreach ($project in $Projects) {
            if (-not (Test-ProjectExists $solutionContent $project.Name)) {
                $guid = $project.Guid
                $configContent += "`t`t{$guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU`r`n"
                $configContent += "`t`t{$guid}.Debug|Any CPU.Build.0 = Debug|Any CPU`r`n"
                $configContent += "`t`t{$guid}.Release|Any CPU.ActiveCfg = Release|Any CPU`r`n"
                $configContent += "`t`t{$guid}.Release|Any CPU.Build.0 = Release|Any CPU`r`n"
            }
        }
        
        $newContent = $configContent + $afterConfig
    }
    
    # Add project nesting relationships
    $nestedSection = "GlobalSection(NestedProjects) = preSolution"
    $nestedIndex = $newContent.IndexOf($nestedSection)
    if ($nestedIndex -ge 0) {
        $nestedEndIndex = $newContent.IndexOf("EndGlobalSection", $nestedIndex)
        $beforeNested = $newContent.Substring(0, $nestedEndIndex)
        $afterNested = $newContent.Substring($nestedEndIndex)
        
        $nestedContent = $beforeNested
        
        foreach ($project in $Projects) {
            if (-not (Test-ProjectExists $solutionContent $project.Name)) {
                $folderName = Get-ProjectFolder $project.Path
                if ($folders.ContainsKey($folderName)) {
                    $folderGuid = $folders[$folderName]
                    $projectGuid = $project.Guid
                    $nestedContent += "`t`t{$projectGuid} = {$folderGuid}`r`n"
                }
            }
        }
        
        $newContent = $nestedContent + $afterNested
    }
    
    # Save solution file
    Set-Content -Path $SolutionPath -Value $newContent -Encoding UTF8
}

# Main function
function Main {
    Write-ColorOutput "=== ModularGodot Framework Project Installation Script ===" "Cyan"
    
    # Determine paths
    if (-not $SolutionPath) {
        $script:SolutionPath = Find-SolutionFile (Get-Location)
        if (-not $script:SolutionPath) {
            Write-ColorOutput "Error: Solution file not found" "Red"
            exit 1
        }
    } else {
        $script:SolutionPath = $SolutionPath
    }
    
    if (-not $CorePath) {
        $scriptDir = Split-Path $PSCommandPath
        $CorePath = Join-Path (Split-Path $scriptDir) "Core"
    }
    
    Write-ColorOutput "Solution file: $script:SolutionPath" "Green"
    Write-ColorOutput "Core directory: $CorePath" "Green"
    
    # Validate paths
    if (-not (Test-Path $script:SolutionPath)) {
        Write-ColorOutput "Error: Solution file does not exist" "Red"
        exit 1
    }
    
    if (-not (Test-Path $CorePath)) {
        Write-ColorOutput "Error: Core directory does not exist" "Red"
        exit 1
    }
    
    # Find all csproj files
    Write-ColorOutput "Scanning project files..." "Yellow"
    $projectFiles = Get-ChildItem -Path $CorePath -Filter "*.csproj" -Recurse
    
    if ($projectFiles.Count -eq 0) {
        Write-ColorOutput "Warning: No project files found" "Yellow"
        exit 0
    }
    
    Write-ColorOutput "Found $($projectFiles.Count) project files" "Green"
    
    # Get project information
    $projects = @()
    foreach ($projectFile in $projectFiles) {
        $projectInfo = Get-ProjectInfo $projectFile.FullName
        $projects += $projectInfo
        Write-ColorOutput "  - $($projectInfo.Name)" "White"
    }
    
    # Add projects to solution
    Write-ColorOutput "Adding projects to solution..." "Yellow"
    Add-ProjectToSolution $script:SolutionPath $projects
    
    Write-ColorOutput "Installation completed!" "Green"
}

# Execute main function
Main