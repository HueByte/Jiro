#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates project structure documentation for the Jiro AI Assistant project.

.DESCRIPTION
    This script uses eza (or erdtree/tree as fallback) to generate a clean project structure 
    that respects .gitignore patterns. The output is formatted as markdown and 
    includes project descriptions and architecture overview.

.PARAMETER OutputPath
    The path where the project structure markdown file will be generated.
    Default: docs/project-structure.md

.PARAMETER UseTreeFallback
    Use the 'tree' command as fallback if 'eza' and 'erdtree' are not available.

.EXAMPLE
    .\generate-project-structure.ps1
    Generates the project structure documentation using default settings.

.EXAMPLE
    .\generate-project-structure.ps1 -OutputPath "docs/structure.md"
    Generates the documentation to a custom path.
#>

param(
    [string]$OutputPath = "docs/project-structure.md",
    [switch]$UseTreeFallback = $false
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get the script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$OutputFile = Join-Path $ProjectRoot $OutputPath

Write-Host "🏗️ Generating project structure documentation..." -ForegroundColor Green
Write-Host "📁 Project root: $ProjectRoot" -ForegroundColor Cyan
Write-Host "📄 Output file: $OutputFile" -ForegroundColor Cyan

# Change to project root directory
Push-Location $ProjectRoot

try {
    # Check if eza is available
    $EzaAvailable = $false
    try {
        $null = Get-Command eza -ErrorAction Stop
        $EzaAvailable = $true
        Write-Host "✅ Found eza command" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ eza command not found" -ForegroundColor Yellow
    }

    # Generate the tree structure
    $TreeOutput = ""
    if ($EzaAvailable -and -not $UseTreeFallback) {
        Write-Host "🌳 Generating tree structure with eza..." -ForegroundColor Blue
        $TreeOutput = eza --tree --git --icons --git-ignore 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠️ eza failed, falling back to basic tree" -ForegroundColor Yellow
            $TreeOutput = eza --tree --icons 2>$null
        }
    }
    else {
        # Check for erdtree on Linux/macOS
        $ErdtreeAvailable = $false
        if (-not ($IsWindows -or $env:OS -eq "Windows_NT")) {
            try {
                $null = Get-Command erdtree -ErrorAction Stop
                $ErdtreeAvailable = $true
                Write-Host "✅ Found erdtree command" -ForegroundColor Green
            }
            catch {
                Write-Host "⚠️ erdtree command not found" -ForegroundColor Yellow
            }
        }

        if ($ErdtreeAvailable) {
            # Use erdtree with git-aware filtering
            Write-Host "🌳 Generating tree structure with erdtree..." -ForegroundColor Blue
            $TreeOutput = erdtree --icons --gitignore --hidden 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Host "⚠️ erdtree failed, falling back to basic erdtree" -ForegroundColor Yellow
                $TreeOutput = erdtree --icons 2>$null
            }
        }
        else {
            # Fallback to tree command (Windows/Linux)
            Write-Host "🌳 Generating tree structure with tree command..." -ForegroundColor Blue
            if ($IsWindows -or $env:OS -eq "Windows_NT") {
                $TreeOutput = tree /F /A 2>$null
            }
            else {
                $TreeOutput = tree -a -I 'bin|obj|_site|_temp|node_modules|.git' 2>$null
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($TreeOutput)) {
        Write-Host "❌ Failed to generate tree structure" -ForegroundColor Red
        $TreeOutput = "Unable to generate tree structure. Please install eza or tree command."
    }

    # Generate current timestamp
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

    # Create the markdown content
    $MarkdownContent = @"
# Jiro Project Structure

This document shows the complete project structure for the Jiro AI Assistant project.
Generated on $Timestamp using automated tooling with git-aware filtering to respect .gitignore patterns.

> ⚠️ **Note**: This file is auto-generated. Do not edit manually as changes will be overwritten.
> To update this documentation, run: ``scripts/generate-project-structure.ps1``

## Key Components

- **src/Jiro.Kernel/**: Main application kernel containing core services and business logic
  - **Jiro.App/**: Console application entry point and gRPC client
  - **Jiro.Core/**: Core domain models, services, and abstractions
  - **Jiro.Infrastructure/**: Data access layer with Entity Framework and repositories
- **src/Jiro.Communication/**: Python communication layer for external integrations and graph generation
- **src/Jiro.Tests/**: Unit and integration tests for all components
- **assets/**: Project assets including images, banners, and documentation diagrams
- **docs/**: Comprehensive project documentation, user guides, and API documentation
- **scripts/**: Build, deployment, and database management scripts
- **api/**: Generated API documentation files (DocFX output)

## Architecture Overview

The project follows Clean Architecture principles with clear separation of concerns:

- Core business logic is isolated in the Core layer
- Infrastructure concerns are handled in the Infrastructure layer
- The App layer serves as the composition root and entry point
- Communication layer provides Python-based external service integration

## Project Tree

``````text
$TreeOutput
``````

## Notable Features

- **Clean Architecture**: Clear separation between Core, Infrastructure, and Application layers
- **Comprehensive Testing**: Unit and integration tests for all major components
- **Multi-language Support**: C# for main application, Python for specialized communication tasks
- **Documentation**: Extensive documentation with DocFX integration
- **Container Support**: Docker configuration for easy deployment
- **Git-aware Structure**: This structure respects .gitignore patterns, excluding build artifacts and temporary files

## Build Artifacts (Excluded)

The following directories and files are excluded from this view due to .gitignore patterns:

- ``bin/`` and ``obj/`` directories
- ``_site/`` and ``_temp/`` DocFX output
- Generated API files (``*.yml``, ``.manifest``)
- User-specific configuration files
- Build and runtime artifacts
- Node modules and package lock files
- IDE-specific files and folders

## Regenerating This Documentation

To regenerate this documentation file:

``````powershell
# From the project root
./scripts/generate-project-structure.ps1

# Or with custom output path
./scripts/generate-project-structure.ps1 -OutputPath "custom/path.md"

# Force tree command fallback
./scripts/generate-project-structure.ps1 -UseTreeFallback
``````

This script is automatically executed during the documentation build process in the GitHub Actions workflow.
"@

    # Ensure the output directory exists
    $OutputDir = Split-Path -Parent $OutputFile
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        Write-Host "📁 Created directory: $OutputDir" -ForegroundColor Green
    }

    # Write the content to file
    $MarkdownContent | Set-Content -Path $OutputFile -Encoding UTF8
    Write-Host "✅ Successfully generated project structure documentation!" -ForegroundColor Green
    Write-Host "📄 File saved to: $OutputFile" -ForegroundColor Cyan

    # Display file size
    $FileSize = (Get-Item $OutputFile).Length
    Write-Host "📊 File size: $($FileSize) bytes" -ForegroundColor Gray

}
catch {
    Write-Host "❌ Error generating project structure: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    # Return to original directory
    Pop-Location
}

Write-Host "🎉 Project structure documentation generation completed!" -ForegroundColor Green
