# Scripts Reference

This document provides an overview of all development and automation scripts available in the Jiro AI Assistant project.

## Overview

The `scripts/` directory contains various PowerShell (`.ps1`) and Bash (`.sh`) scripts for development workflow automation, documentation generation, testing, and release management. All scripts are cross-platform compatible.

## Development Environment Scripts

### setup-dev.ps1 / setup-dev.sh

**Purpose**: Automated development environment setup for new contributors

**What it does**:

- Checks for required development tools (.NET SDK, Git, Node.js, Docker, PowerShell Core)
- Installs global .NET tools (DocFX) and Node.js tools (markdownlint-cli)
- Copies configuration files from examples (`.env`, `appsettings.json`)
- Restores .NET project dependencies
- Generates a `DEV-SETUP.md` guide with manual configuration steps

**Usage**:

```powershell
# PowerShell
.\scripts\setup-dev.ps1

# With options
.\scripts\setup-dev.ps1 -Force -SkipToolCheck
```

```bash
# Bash
./scripts/setup-dev.sh

# With options
./scripts/setup-dev.sh --force --skip-tool-check
```

**Parameters**:

- `-SkipToolCheck` / `--skip-tool-check`: Skip checking for required development tools
- `-SkipDependencies` / `--skip-dependencies`: Skip installing project dependencies
- `-Force` / `--force`: Overwrite existing configuration files
- `-Verbose` / `--verbose`: Show detailed output during setup

## Documentation Scripts

### docfx-gen.ps1 / docfx-gen.sh

**Purpose**: Generate API documentation using DocFX

**What it does**:

- Builds the DocFX documentation site from `src/docfx.json`
- Outputs generated documentation to `src/_site/`
- Supports both build-only and build-and-serve modes

**Usage**:

```powershell
# PowerShell
.\scripts\docfx-gen.ps1           # Build only
.\scripts\docfx-gen.ps1 -Serve    # Build and serve locally
```

```bash
# Bash
./scripts/docfx-gen.sh           # Build only
./scripts/docfx-gen.sh --serve   # Build and serve locally
```

**Parameters**:

- `-Serve` / `--serve`: Start a local web server after building documentation

### markdown-lint.ps1 / markdown-lint.sh

**Purpose**: Lint markdown files for consistency and quality

**What it does**:

- Runs markdownlint-cli on all markdown files in the project
- Uses configuration from `src/.markdownlint.json`
- Ignores generated files and build directories
- Supports fix mode for automatic corrections

**Usage**:

```powershell
# PowerShell
.\scripts\markdown-lint.ps1        # Check only
.\scripts\markdown-lint.ps1 -Fix   # Check and fix issues
```

```bash
# Bash
./scripts/markdown-lint.sh        # Check only
./scripts/markdown-lint.sh --fix  # Check and fix issues
```

**Parameters**:

- `-Fix` / `--fix`: Automatically fix markdown issues where possible

### generate-project-structure.ps1 / generate-project-structure.sh

**Purpose**: Generate and update project structure documentation

**What it does**:

- Uses `eza` (modern `ls` replacement) to generate a tree view of the project
- Auto-installs `eza` if not present (via cargo or winget)
- Outputs structure to `docs/project-structure.md`
- Filters out common ignore patterns (node_modules, .git, build artifacts)

**Usage**:

```powershell
# PowerShell
.\scripts\generate-project-structure.ps1
```

```bash
# Bash
./scripts/generate-project-structure.sh
```

## Testing and CI Scripts

### local-ci-test.ps1 / local-ci-test.sh

**Purpose**: Run the complete CI/CD pipeline locally for testing

**What it does**:

- Builds the .NET solution
- Runs all unit tests
- Generates documentation
- Lints markdown files
- Validates project structure
- Provides comprehensive error reporting

**Usage**:

```powershell
# PowerShell
.\scripts\local-ci-test.ps1                    # Run all tests
.\scripts\local-ci-test.ps1 -SkipBuild         # Skip build step
.\scripts\local-ci-test.ps1 -SkipTests         # Skip unit tests
.\scripts\local-ci-test.ps1 -SkipDocs          # Skip documentation tests
```

```bash
# Bash
./scripts/local-ci-test.sh                    # Run all tests
./scripts/local-ci-test.sh --skip-build       # Skip build step
./scripts/local-ci-test.sh --skip-tests       # Skip unit tests
./scripts/local-ci-test.sh --skip-docs        # Skip documentation tests
```

**Parameters**:

- `-SkipBuild` / `--skip-build`: Skip the build step
- `-SkipTests` / `--skip-tests`: Skip running unit tests
- `-SkipDocs` / `--skip-docs`: Skip documentation generation and linting
- `-Verbose` / `--verbose`: Show detailed output

## Release Management Scripts

### create-release.ps1 / create-release.sh

**Purpose**: Automate the release process

**What it does**:

- Validates the current state of the repository
- Updates version numbers in project files
- Creates git tags for releases
- Generates release notes
- Prepares release artifacts

**Usage**:

```powershell
# PowerShell
.\scripts\create-release.ps1 -Version "1.2.3"
```

```bash
# Bash
./scripts/create-release.sh --version "1.2.3"
```

## Script Dependencies

### Required Tools

- **.NET SDK**: Required for building and running .NET projects
- **Git**: Required for version control operations
- **PowerShell Core** (recommended): For cross-platform PowerShell script execution

### Optional Tools

- **Node.js**: Required for markdown linting (markdownlint-cli)
- **Docker**: Required for containerized development and testing
- **Cargo** (Rust): For installing `eza` on systems without package managers

### Auto-Installed Tools

The following tools are automatically installed by the scripts when needed:

- **DocFX**: .NET documentation generation tool
- **markdownlint-cli**: Markdown linting tool (if Node.js is available)
- **eza**: Modern directory listing tool

## Cross-Platform Compatibility

All scripts are designed to work across platforms:

- **Windows**: Use PowerShell scripts (`.ps1`) with PowerShell Core or Windows PowerShell
- **Linux/macOS**: Use Bash scripts (`.sh`) or PowerShell scripts with PowerShell Core
- **Path handling**: Scripts automatically detect and use appropriate path separators
- **Tool installation**: Scripts use platform-appropriate package managers (winget, cargo, apt, brew, etc.)

## Configuration Files

Scripts use and create the following configuration files:

- `src/.env`: Environment variables (copied from `src/.env.example`)
- `src/Jiro.Kernel/Jiro.App/appsettings.json`: Application settings (copied from example)
- `src/.markdownlint.json`: Markdown linting configuration
- `src/docfx.json`: DocFX documentation configuration
- `DEV-SETUP.md`: Generated development setup guide (ignored by git)

## Error Handling

All scripts include comprehensive error handling:

- **Validation**: Check for required tools and files before execution
- **Fallbacks**: Provide alternative methods when primary tools are unavailable
- **Informative output**: Clear success, warning, and error messages with color coding
- **Exit codes**: Proper exit codes for CI/CD integration
- **Cleanup**: Automatic cleanup of temporary files and state restoration

## Integration with CI/CD

The scripts are designed to integrate seamlessly with GitHub Actions and other CI/CD systems:

- **GitHub Actions**: Used in `.github/workflows/` for automated testing and deployment
- **Exit codes**: Proper exit codes for pipeline success/failure detection
- **Logging**: Structured output compatible with CI/CD log parsing
- **Environment detection**: Automatic detection of CI/CD environments

## Best Practices

When using or modifying these scripts:

1. **Test locally**: Always run `local-ci-test` before pushing changes
2. **Cross-platform testing**: Test scripts on different operating systems when possible
3. **Error handling**: Add appropriate error handling for new functionality
4. **Documentation**: Update this reference when adding new scripts or parameters
5. **Consistency**: Follow the established patterns for parameters and output formatting

## Troubleshooting

### Common Issues

1. **Tool not found**: Run `setup-dev` to install required tools
2. **Permission denied**: Make sure Bash scripts are executable (`chmod +x`)
3. **Path issues**: Use the scripts from the project root directory
4. **Network issues**: Some tools require internet access for installation

### Getting Help

- Run any script with `-h` or `--help` for usage information
- Check the `DEV-SETUP.md` file generated by `setup-dev` for configuration guidance
- Review the CI/CD logs in GitHub Actions for debugging pipeline issues

For more detailed information about specific workflows, see [Workflow Pipelines](workflow-pipelines.md).
