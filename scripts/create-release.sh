#!/bin/bash

# Create Release Script
# This script creates a release and tag when merging from dev to main

VERSION=""
DRY_RUN=false
HELP=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        -h|--help)
            HELP=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

if [ "$HELP" = true ]; then
    cat << EOF
Create Release Script

Usage: ./create-release.sh [options]

Options:
    -v, --version <version>    Specify version (e.g., "v1.2.3")
    --dry-run                 Show what would be done without executing
    -h, --help                Show this help message

Examples:
    ./create-release.sh                      # Auto-generate version
    ./create-release.sh -v "v1.2.3"         # Use specific version
    ./create-release.sh --dry-run            # Preview actions
EOF
    exit 0
fi

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

function log_info() {
    echo -e "${CYAN}$1${NC}"
}

function log_success() {
    echo -e "${GREEN}$1${NC}"
}

function log_warning() {
    echo -e "${YELLOW}$1${NC}"
}

function log_error() {
    echo -e "${RED}$1${NC}"
}

# Check if we're in a git repository
if [ ! -d ".git" ]; then
    log_error "❌ Error: Not in a git repository"
    exit 1
fi

# Check current branch
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" != "main" ]; then
    log_warning "⚠️ Warning: You are not on the main branch (current: $CURRENT_BRANCH)"
    read -p "Do you want to continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_warning "Aborted by user"
        exit 0
    fi
fi

# Auto-generate version if not provided
if [ -z "$VERSION" ]; then
    log_info "🔍 Auto-generating version..."
    
    # Try to get version from .csproj files
    PROJECT_VERSION=$(find . -name "*.csproj" -exec grep -h "<Version>" {} \; | head -1 | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/' | tr -d '[:space:]')
    
    if [ -n "$PROJECT_VERSION" ]; then
        VERSION="v$PROJECT_VERSION"
        log_success "Found version in project file: $PROJECT_VERSION"
    else
        # Fallback to date + commit hash
        DATE=$(date +'%Y.%m.%d')
        COMMIT_HASH=$(git rev-parse --short HEAD)
        VERSION="v$DATE-$COMMIT_HASH"
        log_warning "No version found in project files, using: $VERSION"
    fi
else
    log_success "Using provided version: $VERSION"
fi

# Check if tag already exists
if git tag -l "$VERSION" | grep -q "$VERSION"; then
    log_error "❌ Error: Tag '$VERSION' already exists"
    exit 1
fi

# Get latest tag for comparison
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")
if [ -z "$LATEST_TAG" ]; then
    LATEST_TAG=$(git rev-list --max-parents=0 HEAD)
    log_warning "No previous tags found, comparing against first commit"
else
    log_info "Latest tag: $LATEST_TAG"
fi

# Generate commit list with better formatting
log_info "📝 Generating release notes..."
if [ -n "$LATEST_TAG" ]; then
    COMMITS_RAW=$(git log "$LATEST_TAG..HEAD" --pretty=format:"%h|%s|%an|%ad" --date=short --reverse)
else
    COMMITS_RAW=$(git log --pretty=format:"%h|%s|%an|%ad" --date=short --reverse)
fi

# Format commits properly
FORMATTED_COMMITS=""
if [ -n "$COMMITS_RAW" ]; then
    while IFS='|' read -r hash message author date; do
        if [ -n "$hash" ] && [ -n "$message" ]; then
            FORMATTED_COMMITS="$FORMATTED_COMMITS- **$message** ([\`$hash\`](https://github.com/huebyte/Jiro/commit/$hash)) by @$author on $date
"
        fi
    done <<< "$COMMITS_RAW"
fi

if [ -z "$FORMATTED_COMMITS" ]; then
    FORMATTED_COMMITS="No commits found."
fi

# Generate changelog link  
CHANGELOG_PATH="https://huebyte.github.io/Jiro/documentation/changelog/$VERSION.html"

# Create release notes
RELEASE_NOTES="## What's Changed

### 📋 Detailed Changelog
For detailed information about changes, new features, and breaking changes, see the [**📖 Changelog**]($CHANGELOG_PATH).

### 🔄 Commits in this release:
$FORMATTED_COMMITS

### ℹ️ Release Information
- **Version**: $VERSION
- **Branch**: $CURRENT_BRANCH
- **Generated on**: $(date -u +'%Y-%m-%d %H:%M:%S UTC')
- **Changelog**: [$CHANGELOG_PATH]($CHANGELOG_PATH)

**Full Changelog**: https://github.com/huebyte/Jiro/compare/$LATEST_TAG...$VERSION"

log_warning "📋 Release Notes Preview:"
echo "$RELEASE_NOTES"

if [ "$DRY_RUN" = true ]; then
    log_warning ""
    log_warning "🔍 DRY RUN - Actions that would be performed:"
    log_info "1. Create git tag: $VERSION"
    log_info "2. Push tag to origin"
    log_info "3. Create GitHub release with above notes"
    log_warning ""
    log_warning "To actually create the release, run without --dry-run flag"
    exit 0
fi

# Confirm before creating
echo ""
log_warning "❓ Create release $VERSION?"
read -p "Enter 'yes' to confirm: " -r
if [ "$REPLY" != "yes" ]; then
    log_warning "Aborted by user"
    exit 0
fi

# Create and push tag
log_info "🏷️ Creating tag $VERSION..."
if git tag -a "$VERSION" -m "Release $VERSION" && git push origin "$VERSION"; then
    log_success "✅ Tag created and pushed successfully"
else
    log_error "❌ Error creating or pushing tag"
    exit 1
fi

# Save release notes to file
RELEASE_NOTES_FILE="release_notes_$VERSION.md"
echo "$RELEASE_NOTES" > "$RELEASE_NOTES_FILE"

log_info "📦 Creating GitHub release..."
log_info "Release notes saved to: $RELEASE_NOTES_FILE"
log_warning "You can now create a GitHub release manually using the GitHub web interface or GitHub CLI"
log_info "GitHub CLI command: gh release create $VERSION --notes-file $RELEASE_NOTES_FILE"

echo ""
log_success "🎉 Release preparation completed!"
log_success "Tag: $VERSION"
log_success "Release notes: $RELEASE_NOTES_FILE"
