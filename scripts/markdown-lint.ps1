#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs markdownlint on all Markdown files in the Jiro AI Assistant repository.

.DESCRIPTION
    This script installs markdownlint-cli if not present and runs it on all Markdown files
    in the repository, using the configuration from .markdownlint.json.

.PARAMETER Fix
    Automatically fix issues that can be fixed automatically.

.PARAMETER Verbose
    Show verbose output during linting.

.PARAMETER Path
    Specific path or file to lint (defaults to all .md files).

.EXAMPLE
    .\markdownlint.ps1
    Run markdownlint on all Markdown files

.EXAMPLE
    .\markdownlint.ps1 -Fix
    Run markdownlint and automatically fix issues

.EXAMPLE
    .\markdownlint.ps1 -Path "docs/*.md"
    Run markdownlint only on files in the docs directory

.EXAMPLE
    .\markdownlint.ps1 -Verbose
    Run markdownlint with verbose output
#>

param(
    [switch]$Fix,
    [switch]$Verbose,
    [string]$Path = "**/*.md"
)

# Colors for output
$ErrorColor = "Red"
$SuccessColor = "Green"
$InfoColor = "Cyan"
$WarningColor = "Yellow"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-NodeInstalled {
    try {
        $null = node --version 2>$null
        return $true
    }
    catch {
        return $false
    }
}

function Test-MarkdownlintInstalled {
    try {
        $null = markdownlint --version 2>$null
        return $true
    }
    catch {
        return $false
    }
}

# Change to repository root
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
Push-Location $repoRoot

try {
    Write-ColorOutput "🔍 Jiro AI Assistant Markdownlint Runner" $InfoColor
    Write-ColorOutput "==============================" $InfoColor
    Write-ColorOutput ""

    # Check if Node.js is installed
    if (-not (Test-NodeInstalled)) {
        Write-ColorOutput "❌ Node.js is not installed or not in PATH" $ErrorColor
        Write-ColorOutput "Please install Node.js from https://nodejs.org/" $WarningColor
        exit 1
    }

    $nodeVersion = node --version
    Write-ColorOutput "✅ Node.js version: $nodeVersion" $SuccessColor

    # Check if markdownlint-cli is installed
    if (-not (Test-MarkdownlintInstalled)) {
        Write-ColorOutput "⚠️  markdownlint-cli not found, installing globally..." $WarningColor
        try {
            npm install -g markdownlint-cli
            Write-ColorOutput "✅ markdownlint-cli installed successfully" $SuccessColor
        }
        catch {
            Write-ColorOutput "❌ Failed to install markdownlint-cli" $ErrorColor
            Write-ColorOutput "Error: $_" $ErrorColor
            exit 1
        }
    }

    $markdownlintVersion = markdownlint --version
    Write-ColorOutput "✅ markdownlint-cli version: $markdownlintVersion" $SuccessColor
    Write-ColorOutput ""

    # Check for markdownlint configuration
    if (Test-Path "src\.markdownlint.json") {
        Write-ColorOutput "📋 Using configuration from src\.markdownlint.json" $InfoColor
        $configFile = "src\.markdownlint.json"
    }
    elseif (Test-Path ".markdownlint.json") {
        Write-ColorOutput "📋 Using configuration from .markdownlint.json (root location)" $InfoColor
        $configFile = ".markdownlint.json"
    }
    else {
        Write-ColorOutput "⚠️  No .markdownlint.json found, using default rules" $WarningColor
        $configFile = $null
    }

    # Build markdownlint command
    $markdownlintArgs = @()
    
    # Add path
    $markdownlintArgs += $Path
    
    # Add ignore patterns
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "node_modules"
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "TestResults"
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "src\_site"
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "src\_temp"
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "src\api"
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "_site"
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "bin"
    $markdownlintArgs += "--ignore"
    $markdownlintArgs += "obj"
    
    # Add configuration file if present
    if ($configFile) {
        $markdownlintArgs += "--config"
        $markdownlintArgs += $configFile
    }
    
    # Add fix flag if requested
    if ($Fix) {
        $markdownlintArgs += "--fix"
        Write-ColorOutput "🔧 Auto-fix mode enabled" $InfoColor
    }

    Write-ColorOutput "🚀 Running markdownlint..." $InfoColor
    Write-ColorOutput "Command: markdownlint $($markdownlintArgs -join ' ')" $InfoColor
    Write-ColorOutput ""

    # Run markdownlint
    try {
        if ($Verbose) {
            & markdownlint @markdownlintArgs
        }
        else {
            $output = & markdownlint @markdownlintArgs 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-ColorOutput "✅ Markdownlint completed successfully!" $SuccessColor
                if ($output) {
                    Write-ColorOutput "Output:" $InfoColor
                    Write-Output $output
                }
            }
            else {
                Write-ColorOutput "❌ Markdownlint found issues:" $ErrorColor
                Write-Output $output
                exit $LASTEXITCODE
            }
        }
    }
    catch {
        Write-ColorOutput "❌ Error running markdownlint: $_" $ErrorColor
        exit 1
    }

    Write-ColorOutput ""
    if ($Fix) {
        Write-ColorOutput "🎉 Markdownlint completed with auto-fix!" $SuccessColor
        Write-ColorOutput "📝 Check the changes and commit if appropriate." $InfoColor
    }
    else {
        Write-ColorOutput "🎉 Markdownlint completed successfully!" $SuccessColor
        Write-ColorOutput "💡 Use -Fix parameter to automatically fix issues." $InfoColor
    }
}
finally {
    Pop-Location
}