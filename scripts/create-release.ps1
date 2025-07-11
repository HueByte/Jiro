# Create Release Script
# This script creates a release and tag when merging from dev to main

param(
    [string]$Version = "",
    [switch]$DryRun = $false,
    [switch]$Help = $false
)

if ($Help) {
    Write-Host @"
Create Release Script

Usage: .\create-release.ps1 [options]

Options:
    -Version <version>    Specify version (e.g., "v1.2.3")
    -DryRun               Show what would be done without executing
    -Help                 Show this help message

Examples:
    .\create-release.ps1                    # Auto-generate version
    .\create-release.ps1 -Version "v1.2.3"  # Use specific version
    .\create-release.ps1 -DryRun            # Preview actions
"@
    exit 0
}

# Colors for output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    
    $colors = @{
        "Red"     = "91"
        "Green"   = "92" 
        "Yellow"  = "93"
        "Blue"    = "94"
        "Magenta" = "95"
        "Cyan"    = "96"
        "White"   = "97"
    }
    
    if ($colors.ContainsKey($Color)) {
        Write-Host "`e[$($colors[$Color])m$Message`e[0m"
    }
    else {
        Write-Host $Message
    }
}

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-ColorOutput "❌ Error: Not in a git repository" "Red"
    exit 1
}

# Check current branch
$currentBranch = git branch --show-current
if ($currentBranch -ne "main") {
    Write-ColorOutput "⚠️ Warning: You are not on the main branch (current: $currentBranch)" "Yellow"
    $continue = Read-Host "Do you want to continue anyway? (y/N)"
    if ($continue -ne "y" -and $continue -ne "Y") {
        Write-ColorOutput "Aborted by user" "Yellow"
        exit 0
    }
}

# Auto-generate version if not provided
if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-ColorOutput "🔍 Auto-generating version..." "Cyan"
    
    # Try to get version from .csproj files
    $csprojFiles = Get-ChildItem -Recurse -Filter "*.csproj"
    $projectVersion = $null
    
    foreach ($file in $csprojFiles) {
        $content = Get-Content $file.FullName
        $versionLine = $content | Where-Object { $_ -match '<Version>(.*)</Version>' }
        if ($versionLine) {
            $projectVersion = $matches[1].Trim()
            Write-ColorOutput "Found version in $($file.Name): $projectVersion" "Green"
            break
        }
    }
    
    if ($projectVersion) {
        $Version = "v$projectVersion"
    }
    else {
        # Fallback to date + commit hash
        $date = Get-Date -Format "yyyy.MM.dd"
        $commitHash = git rev-parse --short HEAD
        $Version = "v$date-$commitHash"
        Write-ColorOutput "No version found in project files, using: $Version" "Yellow"
    }
}
else {
    Write-ColorOutput "Using provided version: $Version" "Green"
}

# Check if tag already exists
$existingTag = git tag -l $Version
if ($existingTag) {
    Write-ColorOutput "❌ Error: Tag '$Version' already exists" "Red"
    exit 1
}

# Get latest tag for comparison
$latestTag = git describe --tags --abbrev=0 2>$null
if (-not $latestTag) {
    $latestTag = git rev-list --max-parents=0 HEAD
    Write-ColorOutput "No previous tags found, comparing against first commit" "Yellow"
}
else {
    Write-ColorOutput "Latest tag: $latestTag" "Cyan"
}

# Generate commit list with better formatting
Write-ColorOutput "📝 Generating release notes..." "Cyan"
if ($latestTag) {
    $commitsRaw = git log "$latestTag..HEAD" --pretty=format:"%h|%s|%an|%ad" --date=short --reverse
}
else {
    $commitsRaw = git log --pretty=format:"%h|%s|%an|%ad" --date=short --reverse
}

# Format commits properly
$formattedCommits = @()
if ($commitsRaw) {
    foreach ($commit in $commitsRaw) {
        if (-not [string]::IsNullOrWhiteSpace($commit)) {
            $parts = $commit -split '\|', 4
            if ($parts.Length -eq 4) {
                $hash = $parts[0].Trim()
                $message = $parts[1].Trim()
                $author = $parts[2].Trim()
                $date = $parts[3].Trim()
                $formattedCommits += "- **$message** ([``$hash``](https://github.com/huebyte/Jiro/commit/$hash)) by @$author on $date"
            }
        }
    }
}

$commits = if ($formattedCommits.Count -gt 0) { $formattedCommits -join "`n" } else { "No commits found." }

# Generate changelog link
$changelogPath = "docs/changelog/$Version.md"

$releaseNotes = @"
## What's Changed

### 📋 Detailed Changelog
For detailed information about changes, new features, and breaking changes, see the [**📖 Changelog**]($changelogPath).

### 🔄 Commits in this release:
$commits

### ℹ️ Release Information
- **Version**: $Version
- **Branch**: $currentBranch  
- **Generated on**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC")
- **Changelog**: [$changelogPath]($changelogPath)

**Full Changelog**: https://github.com/huebyte/Jiro/compare/$latestTag...$Version
"@

Write-ColorOutput "📋 Release Notes Preview:" "Yellow"
Write-Host $releaseNotes

if ($DryRun) {
    Write-ColorOutput "`n🔍 DRY RUN - Actions that would be performed:" "Yellow"
    Write-ColorOutput "1. Create git tag: $Version" "Cyan"
    Write-ColorOutput "2. Push tag to origin" "Cyan"
    Write-ColorOutput "3. Create GitHub release with above notes" "Cyan"
    Write-ColorOutput "`nTo actually create the release, run without -DryRun flag" "Yellow"
    exit 0
}

# Confirm before creating
Write-ColorOutput "`n❓ Create release $Version?" "Yellow"
$confirm = Read-Host "Enter 'yes' to confirm"
if ($confirm -ne "yes") {
    Write-ColorOutput "Aborted by user" "Yellow"
    exit 0
}

# Create and push tag
Write-ColorOutput "🏷️ Creating tag $Version..." "Cyan"
try {
    git tag -a $Version -m "Release $Version"
    git push origin $Version
    Write-ColorOutput "✅ Tag created and pushed successfully" "Green"
}
catch {
    Write-ColorOutput "❌ Error creating or pushing tag: $_" "Red"
    exit 1
}

# Save release notes to file
$releaseNotesFile = "release_notes_$Version.md"
$releaseNotes | Out-File -FilePath $releaseNotesFile -Encoding UTF8

Write-ColorOutput "📦 Creating GitHub release..." "Cyan"
Write-ColorOutput "Release notes saved to: $releaseNotesFile" "Cyan"
Write-ColorOutput "You can now create a GitHub release manually using the GitHub web interface or GitHub CLI" "Yellow"
Write-ColorOutput "GitHub CLI command: gh release create $Version --notes-file $releaseNotesFile" "Cyan"

Write-ColorOutput "`n🎉 Release preparation completed!" "Green"
Write-ColorOutput "Tag: $Version" "Green"
Write-ColorOutput "Release notes: $releaseNotesFile" "Green"
