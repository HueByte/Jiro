#!/bin/bash
# 
# Generate project structure documentation for Jiro AI Assistant project
# This script serves as a Linux/macOS compatible version of the PowerShell script
# Uses eza (or erdtree/tree as fallback) to generate clean project structure
#

set -euo pipefail

# Configuration
OUTPUT_PATH="${1:-docs/project-structure.md}"
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_FILE="$PROJECT_ROOT/$OUTPUT_PATH"
TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')

echo "🏗️  Generating project structure documentation..."
echo "📁 Project root: $PROJECT_ROOT"
echo "📄 Output file: $OUTPUT_FILE"

# Change to project root
cd "$PROJECT_ROOT"

# Check for eza command
if command -v eza &> /dev/null; then
    echo "✅ Found eza command"
    TREE_OUTPUT=$(eza --tree --git --icons --git-ignore 2>/dev/null || eza --tree --icons 2>/dev/null || echo "Failed to generate tree with eza")
elif command -v erdtree &> /dev/null; then
    echo "✅ Found erdtree command"
    TREE_OUTPUT=$(erdtree --icons --gitignore --hidden 2>/dev/null || erdtree --icons 2>/dev/null || echo "Failed to generate tree with erdtree")
elif command -v tree &> /dev/null; then
    echo "⚠️  eza and erdtree not found, using tree command"
    TREE_OUTPUT=$(tree -a -I 'bin|obj|_site|_temp|node_modules|.git|*.dll|*.exe|*.pdb|packages' 2>/dev/null || echo "Failed to generate tree with tree command")
else
    echo "❌ No tree generation command found (eza, erdtree, or tree)"
    TREE_OUTPUT="Unable to generate tree structure. Please install eza, erdtree, or tree command."
fi

# Ensure output directory exists
mkdir -p "$(dirname "$OUTPUT_FILE")"

# Generate markdown content
cat > "$OUTPUT_FILE" << EOF
# Jiro Project Structure

This document shows the complete project structure for the Jiro AI Assistant project.
Generated on $TIMESTAMP using automated tooling with git-aware filtering to respect .gitignore patterns.

> ⚠️ **Note**: This file is auto-generated. Do not edit manually as changes will be overwritten.
> To update this documentation, run: \`scripts/generate-project-structure.sh\`

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

\`\`\`text
$TREE_OUTPUT
\`\`\`

## Notable Features

- **Clean Architecture**: Clear separation between Core, Infrastructure, and Application layers
- **Comprehensive Testing**: Unit and integration tests for all major components
- **Multi-language Support**: C# for main application, Python for specialized communication tasks
- **Documentation**: Extensive documentation with DocFX integration
- **Container Support**: Docker configuration for easy deployment
- **Git-aware Structure**: This structure respects .gitignore patterns, excluding build artifacts and temporary files

## Build Artifacts (Excluded)

The following directories and files are excluded from this view due to .gitignore patterns:

- \`bin/\` and \`obj/\` directories
- \`_site/\` and \`_temp/\` DocFX output
- Generated API files (\`*.yml\`, \`.manifest\`)
- User-specific configuration files
- Build and runtime artifacts
- Node modules and package lock files
- IDE-specific files and folders

## Regenerating This Documentation

To regenerate this documentation file:

\`\`\`bash
# From the project root (Linux/macOS)
./scripts/generate-project-structure.sh

# Or with custom output path
./scripts/generate-project-structure.sh "custom/path.md"
\`\`\`

\`\`\`powershell
# From the project root (Windows/PowerShell)
./scripts/generate-project-structure.ps1

# Or with custom output path
./scripts/generate-project-structure.ps1 -OutputPath "custom/path.md"
\`\`\`

This script is automatically executed during the documentation build process in the GitHub Actions workflow.
EOF

echo "✅ Successfully generated project structure documentation!"
echo "📄 File saved to: $OUTPUT_FILE"
echo "📊 File size: $(wc -c < "$OUTPUT_FILE") bytes"
echo "🎉 Project structure documentation generation completed!"
