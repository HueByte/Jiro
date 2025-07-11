#!/bin/bash
# 
# Generate project structure documentation for Jiro AI Assistant project
# This script serves as a Linux/macOS compatible version of the PowerShell script
# Uses eza to generate clean project structure with automatic installation if needed
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

# Check for eza command and install if not found
if command -v eza &> /dev/null; then
    echo "✅ Found eza command"
    # Set locale to ensure proper UTF-8 handling
    export LC_ALL=C.UTF-8
    export LANG=C.UTF-8
    
    # Try with git-ignore first, with level limit and proper encoding
    TREE_OUTPUT=$(eza --tree --git --git-ignore --level 4 --color=never 2>/dev/null | tr -d '\r' || eza --tree --level 4 --color=never 2>/dev/null | tr -d '\r' || echo "Failed to generate tree with eza")
    
    # Check if output contains problematic characters and fall back to tree if needed
    if [[ "$TREE_OUTPUT" == *"Failed"* ]] || [[ -z "$TREE_OUTPUT" ]]; then
        echo "⚠️  eza failed, falling back to tree command"
        TREE_OUTPUT=$(tree -a -I 'bin|obj|_site|_temp|node_modules|.git|*.dll|*.exe|*.pdb|packages' 2>/dev/null | tr -d '\r' || echo "Failed to generate tree with tree command")
    fi
else
    echo "⚠️  eza command not found, attempting to install..."
    
    # Try to install eza via cargo
    if command -v cargo &> /dev/null; then
        echo "📦 Installing eza via cargo..."
        if cargo install eza; then
            echo "✅ Successfully installed eza via cargo"
            # Try to use eza after installation
            if command -v eza &> /dev/null; then
                export LC_ALL=C.UTF-8
                export LANG=C.UTF-8
                TREE_OUTPUT=$(eza --tree --git --git-ignore --level 4 --color=never 2>/dev/null | tr -d '\r' || eza --tree --level 4 --color=never 2>/dev/null | tr -d '\r' || echo "Failed to generate tree with eza")
            else
                echo "⚠️  eza not found in PATH after installation, using tree fallback"
                TREE_OUTPUT=$(tree -a -I 'bin|obj|_site|_temp|node_modules|.git|*.dll|*.exe|*.pdb|packages' 2>/dev/null | tr -d '\r' || echo "Failed to generate tree with tree command")
            fi
        else
            echo "❌ Failed to install eza via cargo, using tree fallback"
            TREE_OUTPUT=$(tree -a -I 'bin|obj|_site|_temp|node_modules|.git|*.dll|*.exe|*.pdb|packages' 2>/dev/null | tr -d '\r' || echo "Failed to generate tree with tree command")
        fi
    else
        echo "❌ cargo not available, cannot install eza. Using tree fallback"
        if command -v tree &> /dev/null; then
            TREE_OUTPUT=$(tree -a -I 'bin|obj|_site|_temp|node_modules|.git|*.dll|*.exe|*.pdb|packages' 2>/dev/null | tr -d '\r' || echo "Failed to generate tree with tree command")
        else
            echo "❌ No tree generation command available"
            TREE_OUTPUT="Unable to generate tree structure. Please install eza via 'cargo install eza' or install tree command."
        fi
    fi
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
