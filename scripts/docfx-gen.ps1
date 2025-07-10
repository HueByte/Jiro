#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs DocFX to build documentation for the Jiro AI Assistant project.

.DESCRIPTION
    This script installs DocFX if not present and builds the documentation using
    the configuration from src/docfx.json. It can build, serve, or clean the documentation.

.PARAMETER Action
    The action to perform: Build, Serve, Clean, or Init (defaults to Build).

.PARAMETER ConfigPath
    Path to the DocFX configuration file (defaults to src/docfx.json).

.PARAMETER Port
    Port number for serving documentation (defaults to 8080).

.PARAMETER Force
    Force rebuild by cleaning first.

.PARAMETER Verbose
    Show verbose output during build.

.EXAMPLE
    .\docfx.ps1
    Build documentation using default configuration

.EXAMPLE
    .\docfx.ps1 -Action Serve
    Build and serve documentation on default port

.EXAMPLE
    .\docfx.ps1 -Action Serve -Port 3000
    Build and serve documentation on port 3000

.EXAMPLE
    .\docfx.ps1 -Action Clean
    Clean generated documentation files

.EXAMPLE
    .\docfx.ps1 -Force -Verbose
    Force rebuild with verbose output
#>

param(
    [ValidateSet("Build", "Serve", "Clean", "Init")]
    [string]$Action = "Build",
    [string]$ConfigPath = "src\docfx.json",
    [int]$Port = 8080,
    [switch]$Force,
    [switch]$Verbose
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

function Test-DocFxInstalled {
    try {
        $null = docfx --version 2>$null
        return $true
    }
    catch {
        return $false
    }
}

function Test-DotNetInstalled {
    try {
        $null = dotnet --version 2>$null
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
    Write-ColorOutput "📚 Jiro AI Assistant DocFX Runner" $InfoColor
    Write-ColorOutput "=======================" $InfoColor
    Write-ColorOutput ""

    # Check if .NET is installed
    if (-not (Test-DotNetInstalled)) {
        Write-ColorOutput "❌ .NET is not installed or not in PATH" $ErrorColor
        Write-ColorOutput "Please install .NET from https://dotnet.microsoft.com/download" $WarningColor
        exit 1
    }

    $dotnetVersion = dotnet --version
    Write-ColorOutput "✅ .NET version: $dotnetVersion" $SuccessColor

    # Check if DocFX is installed
    if (-not (Test-DocFxInstalled)) {
        Write-ColorOutput "⚠️  DocFX not found, installing globally..." $WarningColor
        try {
            dotnet tool install -g docfx --version 2.78.3
            Write-ColorOutput "✅ DocFX installed successfully" $SuccessColor
        }
        catch {
            Write-ColorOutput "❌ Failed to install DocFX" $ErrorColor
            Write-ColorOutput "Error: $_" $ErrorColor
            exit 1
        }
    }

    $docfxVersion = docfx --version
    Write-ColorOutput "✅ DocFX version: $docfxVersion" $SuccessColor
    Write-ColorOutput ""

    # Check for DocFX configuration
    if (-not (Test-Path $ConfigPath)) {
        Write-ColorOutput "❌ DocFX configuration file not found: $ConfigPath" $ErrorColor
        Write-ColorOutput "Please ensure the configuration file exists." $WarningColor
        exit 1
    }

    Write-ColorOutput "📋 Using configuration: $ConfigPath" $InfoColor
    Write-ColorOutput "🎯 Action: $Action" $InfoColor

    # Handle different actions
    switch ($Action) {
        "Clean" {
            Write-ColorOutput "🧹 Cleaning generated files..." $InfoColor
            
            # Remove generated directory
            $configDir = Split-Path -Parent $ConfigPath
            $generatedPath = Join-Path $repoRoot "src\_site"
            
            if (Test-Path $generatedPath) {
                Remove-Item -Recurse -Force $generatedPath
                Write-ColorOutput "✅ Removed _site directory" $SuccessColor
            }
            
            # Remove API docs if they exist
            $apiPath = Join-Path $configDir "api"
            if (Test-Path $apiPath) {
                Remove-Item -Recurse -Force $apiPath
                Write-ColorOutput "✅ Removed API documentation" $SuccessColor
            }
            
            Write-ColorOutput "🎉 Cleanup completed!" $SuccessColor
            return
        }
        
        "Init" {
            Write-ColorOutput "🚀 Initializing DocFX project..." $InfoColor
            $initDir = Read-Host "Enter directory name for new DocFX project (default: docfx-init)"
            if ([string]::IsNullOrWhiteSpace($initDir)) {
                $initDir = "docfx-init"
            }
            
            docfx init -q -o $initDir
            Write-ColorOutput "✅ DocFX project initialized in $initDir" $SuccessColor
            return
        }
        
        "Build" {
            # Force clean if requested
            if ($Force) {
                Write-ColorOutput "🧹 Force cleaning before build..." $InfoColor
                & $MyInvocation.MyCommand.Path -Action Clean -ConfigPath $ConfigPath
                Write-ColorOutput ""
            }
            
            Write-ColorOutput "🔨 Building documentation..." $InfoColor
            
            $docfxArgs = @($ConfigPath)
            if ($Verbose) {
                $docfxArgs += "--verbose"
            }
            
            try {
                & docfx @docfxArgs
                if ($LASTEXITCODE -eq 0) {
                    Write-ColorOutput "✅ Documentation built successfully!" $SuccessColor
                    
                    # Show output location
                    $generatedPath = Join-Path $repoRoot "src\_site"
                    if (Test-Path $generatedPath) {
                        Write-ColorOutput "📁 Output location: $generatedPath" $InfoColor
                        $indexPath = Join-Path $generatedPath "index.html"
                        if (Test-Path $indexPath) {
                            Write-ColorOutput "🌐 Open: file:///$($indexPath.Replace('\', '/'))" $InfoColor
                        }
                    }
                }
                else {
                    Write-ColorOutput "❌ Documentation build failed" $ErrorColor
                    exit $LASTEXITCODE
                }
            }
            catch {
                Write-ColorOutput "❌ Error building documentation: $_" $ErrorColor
                exit 1
            }
        }
        
        "Serve" {
            # Build first
            Write-ColorOutput "🔨 Building documentation before serving..." $InfoColor
            
            $buildArgs = @($ConfigPath)
            if ($Verbose) {
                $buildArgs += "--verbose"
            }
            
            try {
                & docfx @buildArgs
                if ($LASTEXITCODE -ne 0) {
                    Write-ColorOutput "❌ Build failed, cannot serve" $ErrorColor
                    exit $LASTEXITCODE
                }
            }
            catch {
                Write-ColorOutput "❌ Error building documentation: $_" $ErrorColor
                exit 1
            }
            
            Write-ColorOutput "🚀 Starting documentation server on port $Port..." $InfoColor
            Write-ColorOutput "🌐 Documentation will be available at: http://localhost:$Port" $SuccessColor
            Write-ColorOutput "⏹️  Press Ctrl+C to stop the server" $InfoColor
            Write-ColorOutput ""
            
            try {
                $serveArgs = @($ConfigPath, "--serve", "--port", $Port.ToString())
                & docfx @serveArgs
            }
            catch {
                Write-ColorOutput "❌ Error serving documentation: $_" $ErrorColor
                exit 1
            }
        }
    }

    Write-ColorOutput ""
    Write-ColorOutput "🎉 DocFX operation completed successfully!" $SuccessColor
    
    if ($Action -eq "Build") {
        Write-ColorOutput "💡 Use -Action Serve to build and serve the documentation locally" $InfoColor
        Write-ColorOutput "💡 Use -Action Clean to remove generated files" $InfoColor
    }
}
finally {
    Pop-Location
}