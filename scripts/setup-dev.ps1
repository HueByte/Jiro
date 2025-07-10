#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sets up the Jiro AI Assistant development environment.

.DESCRIPTION
    This script prepares your local development environment for the Jiro AI Assistant project.
    It checks for required tools, copies configuration files, installs dependencies, and provides
    guidance for manual configuration steps.

.PARAMETER SkipToolCheck
    Skip checking for required development tools.

.PARAMETER SkipDependencies
    Skip installing project dependencies.

.PARAMETER Force
    Overwrite existing configuration files.

.PARAMETER Verbose
    Show detailed output during setup.

.EXAMPLE
    .\setup-dev.ps1
    Run full development environment setup

.EXAMPLE
    .\setup-dev.ps1 -Force
    Setup and overwrite existing config files

.EXAMPLE
    .\setup-dev.ps1 -SkipToolCheck
    Skip tool installation checks
#>

param(
    [switch]$SkipToolCheck,
    [switch]$SkipDependencies,
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

function Write-Header {
    param([string]$Message)
    Write-ColorOutput "" 
    Write-ColorOutput "========================================" $InfoColor
    Write-ColorOutput $Message $InfoColor
    Write-ColorOutput "========================================" $InfoColor
}

function Write-Step {
    param([string]$Message)
    Write-ColorOutput "🔧 $Message" $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" $SuccessColor
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠️ $Message" $WarningColor
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" $ErrorColor
}

function Test-Command {
    param([string]$Command)
    try {
        $null = Get-Command $Command -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Install-GlobalTool {
    param(
        [string]$Tool,
        [string]$Package,
        [string]$InstallCommand
    )
    
    if (Test-Command $Tool) {
        Write-Success "$Tool is already installed"
        return $true
    }
    
    Write-Step "Installing $Tool..."
    try {
        if ($InstallCommand) {
            Invoke-Expression $InstallCommand
        }
        else {
            dotnet tool install -g $Package
        }
        
        if (Test-Command $Tool) {
            Write-Success "$Tool installed successfully"
            return $true
        }
        else {
            Write-Warning "$Tool installation completed but not found in PATH. You may need to restart your terminal."
            return $false
        }
    }
    catch {
        Write-Error "Failed to install $Tool`: $($_.Exception.Message)"
        return $false
    }
}

function Copy-ConfigFile {
    param(
        [string]$Source,
        [string]$Destination,
        [string]$Description
    )
    
    if (Test-Path $Source) {
        if ((Test-Path $Destination) -and -not $Force) {
            Write-Warning "$Description already exists at $Destination (use -Force to overwrite)"
            return $false
        }
        
        try {
            Copy-Item $Source $Destination -Force
            Write-Success "Created $Description at $Destination"
            return $true
        }
        catch {
            Write-Error "Failed to copy $Description`: $($_.Exception.Message)"
            return $false
        }
    }
    else {
        Write-Warning "Source file not found: $Source"
        return $false
    }
}

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Change to project root
Push-Location $ProjectRoot

try {
    Write-Header "Jiro AI Assistant - Development Environment Setup"
    Write-ColorOutput "This script will prepare your development environment for the Jiro AI Assistant project." $InfoColor
    Write-ColorOutput ""

    # Check for required tools
    if (-not $SkipToolCheck) {
        Write-Header "Checking Required Development Tools"
        
        $toolsToCheck = @{
            ".NET SDK"        = @{
                Command        = "dotnet"
                VersionCommand = "dotnet --version"
                InstallUrl     = "https://dotnet.microsoft.com/download"
                Required       = $true
            }
            "Git"             = @{
                Command        = "git"
                VersionCommand = "git --version"
                InstallUrl     = "https://git-scm.com/downloads"
                Required       = $true
            }
            "Node.js"         = @{
                Command        = "node"
                VersionCommand = "node --version"
                InstallUrl     = "https://nodejs.org/"
                Required       = $false
            }
            "Docker"          = @{
                Command        = "docker"
                VersionCommand = "docker --version"
                InstallUrl     = "https://www.docker.com/get-started"
                Required       = $false
            }
            "PowerShell Core" = @{
                Command        = "pwsh"
                VersionCommand = "pwsh --version"
                InstallUrl     = "https://github.com/PowerShell/PowerShell#get-powershell"
                Required       = $false
            }
        }
        
        $missingRequired = @()
        $missingOptional = @()
        
        foreach ($toolName in $toolsToCheck.Keys) {
            $tool = $toolsToCheck[$toolName]
            Write-Step "Checking $toolName..."
            
            if (Test-Command $tool.Command) {
                try {
                    $version = Invoke-Expression $tool.VersionCommand 2>$null
                    Write-Success "$toolName is installed - $version"
                }
                catch {
                    Write-Success "$toolName is installed"
                }
            }
            else {
                if ($tool.Required) {
                    $missingRequired += $toolName
                    Write-Error "$toolName is required but not found"
                }
                else {
                    $missingOptional += $toolName
                    Write-Warning "$toolName is optional but recommended"
                }
                Write-ColorOutput "   Install from: $($tool.InstallUrl)" $InfoColor
            }
        }
        
        if ($missingRequired.Count -gt 0) {
            Write-Error "Missing required tools: $($missingRequired -join ', ')"
            Write-ColorOutput "Please install the required tools and run this script again." $ErrorColor
            exit 1
        }
        
        # Install global .NET tools
        Write-Header "Installing Global .NET Tools"
        
        $toolInstallResults = @{}
        $toolInstallResults["DocFX"] = Install-GlobalTool "docfx" "docfx" "dotnet tool install -g docfx --version 2.75.3"
        
        # Install Node.js tools if Node is available
        if (Test-Command "node") {
            Write-Step "Installing markdownlint-cli..."
            try {
                npm install -g markdownlint-cli 2>$null
                if (Test-Command "markdownlint") {
                    Write-Success "markdownlint-cli installed successfully"
                    $toolInstallResults["markdownlint"] = $true
                }
                else {
                    Write-Warning "markdownlint-cli installation completed but not found in PATH"
                    $toolInstallResults["markdownlint"] = $false
                }
            }
            catch {
                Write-Warning "Failed to install markdownlint-cli. You may need to install it manually: npm install -g markdownlint-cli"
                $toolInstallResults["markdownlint"] = $false
            }
        }
        else {
            Write-Warning "Node.js not available, skipping markdownlint-cli installation"
            $toolInstallResults["markdownlint"] = $false
        }
    }
    
    # Copy configuration files
    Write-Header "Setting Up Configuration Files"
    
    $configFiles = @(
        @{
            Source      = "src\.env.example"
            Destination = "src\.env"
            Description = "Environment configuration"
        },
        @{
            Source      = "src\Jiro.Kernel\Jiro.App\appsettings.example.json"
            Destination = "src\Jiro.Kernel\Jiro.App\appsettings.json"
            Description = "Application settings"
        }
    )
    
    $copiedFiles = @()
    foreach ($config in $configFiles) {
        if (Copy-ConfigFile $config.Source $config.Destination $config.Description) {
            $copiedFiles += $config.Destination
        }
    }
    
    # Install project dependencies
    if (-not $SkipDependencies) {
        Write-Header "Installing Project Dependencies"
        
        if (Test-Path "src\Main.sln") {
            Write-Step "Restoring .NET packages..."
            try {
                dotnet restore "src\Main.sln"
                Write-Success ".NET packages restored successfully"
            }
            catch {
                Write-Error "Failed to restore .NET packages: $($_.Exception.Message)"
            }
        }
        else {
            Write-Warning "Main.sln not found, skipping .NET package restore"
        }
    }
    
    # Create development configuration file
    Write-Header "Creating Development Configuration Guide"
    
    $devConfigContent = @"
# Jiro AI Assistant - Development Configuration

This file contains all the configuration values you need to set manually for development.
After running setup-dev, please review and update these settings.

## Configuration Files Created:
$(if ($copiedFiles.Count -gt 0) { ($copiedFiles | ForEach-Object { "- $_" }) -join "`n" } else { "- None" })

## Environment Variables (.env)
Location: src/.env

Required settings to update:
- MYSQL_ROOT_PASSWORD: Set a secure root password for MySQL
- MYSQL_PASSWORD: Set a secure password for the jiro_user
- ASPNETCORE_ENVIRONMENT: Set to 'Development' for local development

## Application Settings (appsettings.json)
Location: src/Jiro.Kernel/Jiro.App/appsettings.json

Required settings to update:
- ConnectionStrings.JiroContext: Database connection string
- JWT.Secret: Change to a secure secret key (minimum 32 characters)
- JWT.Issuer: Set to your domain or 'localhost' for development
- JWT.Audience: Set to your domain or 'localhost' for development
- TokenizerUrl: Update if your tokenizer service runs on a different port

## Development Tools Status:
$(if (-not $SkipToolCheck) {
    "- DocFX: $(if ($toolInstallResults["DocFX"]) { "✅ Installed" } else { "❌ Failed" })"
    "- markdownlint-cli: $(if ($toolInstallResults["markdownlint"]) { "✅ Installed" } else { "❌ Failed or Node.js not available" })"
} else {
    "- Tool check was skipped"
})

## Next Steps:
1. Review and update the configuration files listed above
2. Set up your database (MySQL)
3. Configure your external services (tokenizer, etc.)
4. Run the project: dotnet run --project src/Jiro.Kernel/Jiro.App
5. Run tests: dotnet test src/Main.sln

## Useful Development Commands:
- Build project: dotnet build src/Main.sln
- Run tests: dotnet test src/Main.sln
- Generate docs: ./scripts/docfx-gen.ps1
- Lint markdown: ./scripts/markdown-lint.ps1
- Local CI test: ./scripts/local-ci-test.ps1

For more information, see the project documentation.
"@
    
    $devConfigFile = "DEV-SETUP.md"
    $devConfigContent | Out-File -FilePath $devConfigFile -Encoding UTF8
    Write-Success "Development configuration guide created: $devConfigFile"
    
    # Final summary
    Write-Header "Setup Complete!"
    
    Write-ColorOutput "Development environment setup completed successfully!" $SuccessColor
    Write-ColorOutput ""
    Write-ColorOutput "📋 Summary:" $InfoColor
    Write-ColorOutput "- Configuration files have been created from examples" $InfoColor
    Write-ColorOutput "- Development tools have been checked and installed where possible" $InfoColor
    Write-ColorOutput "- Project dependencies have been restored" $InfoColor
    Write-ColorOutput "- Development guide created: $devConfigFile" $InfoColor
    Write-ColorOutput ""
    Write-ColorOutput "🔧 Next Steps:" $WarningColor
    Write-ColorOutput "1. Review and update configuration files (see $devConfigFile)" $WarningColor
    Write-ColorOutput "2. Set up your database" $WarningColor
    Write-ColorOutput "3. Configure external services" $WarningColor
    Write-ColorOutput "4. Test the setup: dotnet run --project src/Jiro.Kernel/Jiro.App" $WarningColor
    Write-ColorOutput ""
    Write-ColorOutput "Happy coding! 🎉" $SuccessColor
}
catch {
    Write-Error "Setup failed: $($_.Exception.Message)"
    exit 1
}
finally {
    Pop-Location
}
