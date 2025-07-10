# GitHub Actions for Eza Setup

This directory contains composite GitHub Actions for setting up `eza` in CI/CD workflows.

## Available Actions

### setup-eza (Linux/macOS)

Located in `.github/actions/setup-eza/`

Installs `eza` on Linux/macOS runners using Rust cargo.

**Usage:**

```yaml
- name: Setup eza
  uses: ./.github/actions/setup-eza
  with:
    force-install: false # Optional: force reinstall even if present
```

### setup-eza-windows (Windows)

Located in `.github/actions/setup-eza-windows/`

Installs `eza` on Windows runners using winget with cargo fallback.

**Usage:**

```yaml
- name: Setup eza
  uses: ./.github/actions/setup-eza-windows
  with:
    force-install: false # Optional: force reinstall even if present
```

## How It Works

1. **Check Existing Installation**: First checks if `eza` is already available
2. **Primary Installation Method**:
   - Linux/macOS: Uses `cargo install eza`
   - Windows: Uses `winget install eza.eza`
3. **Fallback**: Windows action falls back to cargo if winget fails
4. **Path Setup**: Ensures `eza` is available in subsequent workflow steps

## Integration with Project Structure Scripts

These actions work seamlessly with the project's structure generation scripts:

- `scripts/generate-project-structure.ps1` (PowerShell)
- `scripts/generate-project-structure.sh` (Bash)

The scripts have built-in fallback logic, so they will:

1. Use `eza` if available (preferred)
2. Try to install `eza` automatically if not found
3. Fall back to `tree` command if installation fails

## Workflow Example

```yaml
name: Generate Documentation

on: [push]

jobs:
  docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup eza
        uses: ./.github/actions/setup-eza
        
      - name: Generate project structure
        run: |
          chmod +x scripts/generate-project-structure.ps1
          pwsh scripts/generate-project-structure.ps1
```

## Benefits

- **Consistent Setup**: Standardized way to install `eza` across workflows
- **Automatic Fallback**: Multiple installation methods ensure reliability
- **Performance**: Skips installation if `eza` is already present
- **Cross-Platform**: Separate actions optimized for different operating systems
