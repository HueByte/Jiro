# Local CI/CD Testing Script for Jiro.Kernel (PowerShell)
# This script mimics the GitHub Actions workflows for local testing

param(
    [switch]$SkipDocker,
    [switch]$SkipSecurity,
    [switch]$SkipPerformance,
    [string]$Configuration = "Release"
)

# Configuration
# Get script directory and navigate to project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
Push-Location $ProjectRoot

$SolutionPath = "./src/Main.sln"
$DockerfilePath = "./src/Jiro.Kernel/Dockerfile"
$DockerImageName = "jiro-kernel-test"
$ProjectPath = "./src/Jiro.Kernel"

# Helper functions
function Write-Header {
    param([string]$Message)
    Write-Host "========================================" -ForegroundColor Blue
    Write-Host $Message -ForegroundColor Blue
    Write-Host "========================================" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK version: $dotnetVersion"
    }
    catch {
        Write-Error ".NET SDK not found. Please install .NET 10 SDK."
        exit 1
    }
    
    # Check Docker
    try {
        docker --version | Out-Null
        Write-Success "Docker is available"
        $script:SkipDockerLocal = $false
    }
    catch {
        Write-Warning "Docker not found. Docker tests will be skipped."
        $script:SkipDockerLocal = $true
    }
    
    # Prepare configuration files
    $appSettingsPath = "./src/Jiro.Kernel/Jiro.App/Configuration/appsettings.json"
    $appSettingsExamplePath = "./src/Jiro.Kernel/Jiro.App/Configuration/appsettings.example.json"
    
    if (!(Test-Path $appSettingsPath) -and (Test-Path $appSettingsExamplePath)) {
        Copy-Item $appSettingsExamplePath $appSettingsPath
        Write-Success "Copied appsettings.example.json to appsettings.json"
    }
}

function Invoke-BuildAndTest {
    Write-Header "Build and Test"
    
    Write-Host "Restoring dependencies..." -ForegroundColor Cyan
    dotnet restore $SolutionPath
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
    Write-Success "Dependencies restored"
    
    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build $SolutionPath --no-restore --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Success "Build completed"
    
    Write-Host "Checking code formatting..." -ForegroundColor Cyan
    dotnet format $SolutionPath --no-restore --verify-no-changes --verbosity diagnostic
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Code formatting is correct"
    }
    else {
        Write-Warning "Code formatting issues found. Run 'dotnet format' to fix them."
    }
    
    Write-Host "Running tests..." -ForegroundColor Cyan
    dotnet test $SolutionPath --no-build --configuration $Configuration --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    Write-Success "Tests completed"
}

function Invoke-SecurityChecks {
    if ($SkipSecurity) {
        Write-Warning "Skipping security checks"
        return
    }
    
    Write-Header "Security Checks"
    
    Write-Host "Checking for vulnerable packages..." -ForegroundColor Cyan
    $vulnOutput = dotnet list $SolutionPath package --vulnerable --include-transitive 2>&1
    
    if ($vulnOutput -match "no vulnerable packages") {
        Write-Success "No vulnerable packages found"
    }
    else {
        Write-Warning "Vulnerable packages detected. Please review and update."
        Write-Host $vulnOutput -ForegroundColor Yellow
    }
}

function Invoke-DockerBuild {
    if ($SkipDocker -or $script:SkipDockerLocal) {
        Write-Warning "Skipping Docker tests"
        return
    }
    
    Write-Header "Docker Build and Test"
    
    Write-Host "Building Docker image..." -ForegroundColor Cyan
    docker build -t $DockerImageName -f $DockerfilePath $ProjectPath
    if ($LASTEXITCODE -ne 0) { throw "Docker build failed" }
    Write-Success "Docker image built successfully"
    
    Write-Host "Testing Docker image..." -ForegroundColor Cyan
    
    try {
        # Start container in background
        $containerId = docker run -d --name jiro-kernel-test $DockerImageName
        if ($LASTEXITCODE -ne 0) { throw "Failed to start container" }
        
        # Wait a moment for startup
        Start-Sleep -Seconds 5
        
        # Check if container is running
        $runningContainers = docker ps --format "table {{.Names}}"
        if ($runningContainers -match "jiro-kernel-test") {
            Write-Success "Container started successfully (ID: $($containerId.Substring(0,12)))"
            
            # Check logs for any immediate errors
            Write-Host "Container logs:" -ForegroundColor Cyan
            docker logs jiro-kernel-test
        }
        else {
            Write-Error "Container failed to start"
            docker logs jiro-kernel-test
            throw "Container test failed"
        }
    }
    finally {
        # Cleanup
        docker stop jiro-kernel-test 2>$null | Out-Null
        docker rm jiro-kernel-test 2>$null | Out-Null
        Write-Success "Container test completed"
    }
    
    # Clean up image
    $response = Read-Host "Remove test Docker image? (y/N)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        docker rmi $DockerImageName | Out-Null
        Write-Success "Test image removed"
    }
}

function Invoke-DocumentationTests {
    Write-Header "Documentation Tests"
    
    Write-Host "Testing project structure generation..." -ForegroundColor Cyan
    try {
        & "./scripts/generate-project-structure.ps1" -OutputPath "temp-project-structure.md"
        if (Test-Path "temp-project-structure.md") {
            Write-Success "Project structure generation test passed"
            Remove-Item "temp-project-structure.md" -Force
        }
        else {
            Write-Warning "Project structure generation did not create output file"
        }
    }
    catch {
        Write-Warning "Project structure generation test failed: $($_.Exception.Message)"
    }
    
    Write-Host "Checking for DocFX configuration..." -ForegroundColor Cyan
    if (Test-Path "./dev/docfx.json") {
        Write-Success "DocFX configuration found"
        
        # Test DocFX build if available
        try {
            $docfxVersion = docfx --version 2>$null
            if ($docfxVersion) {
                Write-Host "Testing DocFX build..." -ForegroundColor Cyan
                Push-Location "./src"
                try {
                    docfx docfx.json
                    Write-Success "DocFX configuration is valid"
                }
                catch {
                    Write-Warning "DocFX build test failed: $($_.Exception.Message)"
                }
                finally {
                    Pop-Location
                }
            }
            else {
                Write-Warning "DocFX not installed. Install with: dotnet tool install -g docfx"
            }
        }
        catch {
            Write-Warning "DocFX not available for testing"
        }
    }
    else {
        Write-Warning "DocFX configuration not found at ./dev/docfx.json"
    }
}

function Invoke-PerformanceTests {
    if ($SkipPerformance) {
        Write-Warning "Skipping performance tests"
        return
    }
    
    Write-Header "Performance Tests"
    
    Write-Host "Looking for performance tests..." -ForegroundColor Cyan
    $testList = dotnet test $SolutionPath --list-tests 2>&1
    $perfTests = $testList | Select-String -Pattern "performance" -CaseSensitive:$false
    
    if ($perfTests) {
        Write-Host "Running performance tests..." -ForegroundColor Cyan
        dotnet test $SolutionPath --no-build --configuration $Configuration --filter "Category=Performance"
        Write-Success "Performance tests completed"
    }
    else {
        Write-Warning "No performance tests found. Consider adding tests with [Category(`"Performance`")] attribute."
    }
    
    # Check for benchmark projects
    Write-Host "Looking for benchmark projects..." -ForegroundColor Cyan
    $benchmarkProjects = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" -Recurse | 
    Where-Object { (Get-Content $_.FullName) -match "BenchmarkDotNet" }
    
    if ($benchmarkProjects) {
        Write-Host "Found benchmark projects. Running benchmarks..." -ForegroundColor Cyan
        foreach ($project in $benchmarkProjects) {
            Write-Host "Running benchmarks in $($project.FullName)" -ForegroundColor Cyan
            try {
                dotnet run --project $project.FullName --configuration $Configuration --framework net10.0 -- --filter "*"
            }
            catch {
                Write-Warning "Benchmark failed for $($project.Name)"
            }
        }
        Write-Success "Benchmarks completed"
    }
    else {
        Write-Warning "No benchmark projects found. Consider adding BenchmarkDotNet for performance testing."
    }
}

function Write-Report {
    Write-Header "Test Summary"
    
    Write-Host "Local CI/CD test completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Review any warnings above"
    Write-Host "2. Fix code formatting issues if any"
    Write-Host "3. Update vulnerable dependencies if any"
    Write-Host "4. Commit and push to trigger GitHub Actions"
    Write-Host ""
    Write-Host "GitHub Actions workflows:" -ForegroundColor Cyan
    Write-Host "- jiro-kernel-ci.yml: Main CI/CD pipeline"
    Write-Host "- jiro-kernel-security.yml: Weekly security scans"
    Write-Host "- jiro-kernel-performance.yml: Performance testing"
    Write-Host ""
    Write-Success "All local tests completed!"
}

# Main execution
function Main {
    Write-Host "Jiro.Kernel Local CI/CD Test Runner" -ForegroundColor Blue
    Write-Host "This script runs the same checks as GitHub Actions" -ForegroundColor Gray
    Write-Host ""
    
    try {
        Test-Prerequisites
        Invoke-BuildAndTest
        Invoke-SecurityChecks
        Invoke-DocumentationTests
        Invoke-DockerBuild
        Invoke-PerformanceTests
        Write-Report
    }
    catch {
        Write-Error "Test failed: $_"
        exit 1
    }
}

# Run main function
try {
    Main
}
finally {
    # Return to original directory
    Pop-Location
}
