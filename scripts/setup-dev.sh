#!/bin/bash

# Jiro AI Assistant - Development Environment Setup Script
# This script prepares your local development environment

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default options
SKIP_TOOL_CHECK=false
SKIP_DEPENDENCIES=false
FORCE=false
VERBOSE=false

# Functions
print_color() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_error() {
    print_color $RED "❌ $1"
}

print_success() {
    print_color $GREEN "✅ $1"
}

print_warning() {
    print_color $YELLOW "⚠️ $1"
}

print_info() {
    print_color $CYAN "🔧 $1"
}

print_header() {
    echo
    print_color $CYAN "========================================"
    print_color $CYAN "$1"
    print_color $CYAN "========================================"
}

check_command() {
    if command -v "$1" >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

install_node_package() {
    local package=$1
    local command=$2
    
    if check_command "$command"; then
        print_success "$command is already installed"
        return 0
    fi
    
    print_info "Installing $package..."
    if npm install -g "$package" >/dev/null 2>&1; then
        if check_command "$command"; then
            print_success "$package installed successfully"
            return 0
        else
            print_warning "$package installation completed but not found in PATH"
            return 1
        fi
    else
        print_error "Failed to install $package"
        return 1
    fi
}

copy_config_file() {
    local source=$1
    local dest=$2
    local description=$3
    
    if [[ -f "$source" ]]; then
        if [[ -f "$dest" ]] && [[ "$FORCE" != true ]]; then
            print_warning "$description already exists at $dest (use --force to overwrite)"
            return 1
        fi
        
        if cp "$source" "$dest"; then
            print_success "Created $description at $dest"
            return 0
        else
            print_error "Failed to copy $description"
            return 1
        fi
    else
        print_warning "Source file not found: $source"
        return 1
    fi
}

show_help() {
    cat << EOF
Jiro AI Assistant - Development Environment Setup

USAGE:
    ./setup-dev.sh [OPTIONS]

OPTIONS:
    --skip-tool-check     Skip checking for required development tools
    --skip-dependencies   Skip installing project dependencies  
    --force              Overwrite existing configuration files
    --verbose            Show detailed output during setup
    -h, --help           Show this help message

EXAMPLES:
    ./setup-dev.sh                    # Run full development environment setup
    ./setup-dev.sh --force            # Setup and overwrite existing config files
    ./setup-dev.sh --skip-tool-check  # Skip tool installation checks

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tool-check)
            SKIP_TOOL_CHECK=true
            shift
            ;;
        --skip-dependencies)
            SKIP_DEPENDENCIES=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

print_header "Jiro AI Assistant - Development Environment Setup"
print_color $CYAN "This script will prepare your development environment for the Jiro AI Assistant project."
echo

# Check for required tools
if [[ "$SKIP_TOOL_CHECK" != true ]]; then
    print_header "Checking Required Development Tools"
    
    declare -A tools=(
        [".NET SDK"]="dotnet|dotnet --version|https://dotnet.microsoft.com/download|required"
        ["Git"]="git|git --version|https://git-scm.com/downloads|required"
        ["Node.js"]="node|node --version|https://nodejs.org/|optional"
        ["Docker"]="docker|docker --version|https://www.docker.com/get-started|optional"
        ["Bash"]="bash|bash --version|Built-in on most systems|optional"
    )
    
    missing_required=()
    missing_optional=()
    
    for tool_name in "${!tools[@]}"; do
        IFS='|' read -ra tool_info <<< "${tools[$tool_name]}"
        command=${tool_info[0]}
        version_cmd=${tool_info[1]}
        install_url=${tool_info[2]}
        required=${tool_info[3]}
        
        print_info "Checking $tool_name..."
        
        if check_command "$command"; then
            if version_output=$($version_cmd 2>/dev/null); then
                print_success "$tool_name is installed - $version_output"
            else
                print_success "$tool_name is installed"
            fi
        else
            if [[ "$required" == "required" ]]; then
                missing_required+=("$tool_name")
                print_error "$tool_name is required but not found"
            else
                missing_optional+=("$tool_name")
                print_warning "$tool_name is optional but recommended"
            fi
            print_color $CYAN "   Install from: $install_url"
        fi
    done
    
    if [[ ${#missing_required[@]} -gt 0 ]]; then
        print_error "Missing required tools: ${missing_required[*]}"
        print_color $RED "Please install the required tools and run this script again."
        exit 1
    fi
    
    # Install global .NET tools
    print_header "Installing Global .NET Tools"
    
    declare -A tool_results
    
    print_info "Installing DocFX..."
    if dotnet tool install -g docfx --version 2.75.3 >/dev/null 2>&1; then
        if check_command "docfx"; then
            print_success "DocFX installed successfully"
            tool_results["DocFX"]="✅ Installed"
        else
            print_warning "DocFX installation completed but not found in PATH"
            tool_results["DocFX"]="❌ Failed (PATH issue)"
        fi
    else
        # Tool might already be installed
        if check_command "docfx"; then
            print_success "DocFX is already installed"
            tool_results["DocFX"]="✅ Already installed"
        else
            print_warning "Failed to install DocFX"
            tool_results["DocFX"]="❌ Failed"
        fi
    fi
    
    # Install Node.js tools if Node is available
    if check_command "node"; then
        if install_node_package "markdownlint-cli" "markdownlint"; then
            tool_results["markdownlint"]="✅ Installed"
        else
            tool_results["markdownlint"]="❌ Failed"
        fi
    else
        print_warning "Node.js not available, skipping markdownlint-cli installation"
        tool_results["markdownlint"]="❌ Node.js not available"
    fi
fi

# Copy configuration files
print_header "Setting Up Configuration Files"

config_files=(
    "src/.env.example|src/.env|Environment configuration"
    "src/Jiro.Kernel/Jiro.App/appsettings.example.json|src/Jiro.Kernel/Jiro.App/appsettings.json|Application settings"
)

copied_files=()
for config in "${config_files[@]}"; do
    IFS='|' read -ra config_info <<< "$config"
    source=${config_info[0]}
    dest=${config_info[1]}
    description=${config_info[2]}
    
    if copy_config_file "$source" "$dest" "$description"; then
        copied_files+=("$dest")
    fi
done

# Install project dependencies
if [[ "$SKIP_DEPENDENCIES" != true ]]; then
    print_header "Installing Project Dependencies"
    
    if [[ -f "src/Main.sln" ]]; then
        print_info "Restoring .NET packages..."
        if dotnet restore "src/Main.sln"; then
            print_success ".NET packages restored successfully"
        else
            print_error "Failed to restore .NET packages"
        fi
    else
        print_warning "Main.sln not found, skipping .NET package restore"
    fi
fi

# Create development configuration file
print_header "Creating Development Configuration Guide"

dev_config_content="# Jiro AI Assistant - Development Configuration

This file contains all the configuration values you need to set manually for development.
After running setup-dev, please review and update these settings.

## Configuration Files Created:"

if [[ ${#copied_files[@]} -gt 0 ]]; then
    for file in "${copied_files[@]}"; do
        dev_config_content="$dev_config_content
- $file"
    done
else
    dev_config_content="$dev_config_content
- None"
fi

dev_config_content="$dev_config_content

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

## Development Tools Status:"

if [[ "$SKIP_TOOL_CHECK" != true ]]; then
    for tool in "${!tool_results[@]}"; do
        dev_config_content="$dev_config_content
- $tool: ${tool_results[$tool]}"
    done
else
    dev_config_content="$dev_config_content
- Tool check was skipped"
fi

dev_config_content="$dev_config_content

## Next Steps:
1. Review and update the configuration files listed above
2. Set up your database (MySQL)
3. Configure your external services (tokenizer, etc.)
4. Run the project: dotnet run --project src/Jiro.Kernel/Jiro.App
5. Run tests: dotnet test src/Main.sln

## Useful Development Commands:
- Build project: dotnet build src/Main.sln
- Run tests: dotnet test src/Main.sln
- Generate docs: ./scripts/docfx-gen.sh
- Lint markdown: ./scripts/markdown-lint.sh
- Local CI test: ./scripts/local-ci-test.sh

For more information, see the project documentation."

dev_config_file="DEV-SETUP.md"
echo "$dev_config_content" > "$dev_config_file"
print_success "Development configuration guide created: $dev_config_file"

# Final summary
print_header "Setup Complete!"

print_color $GREEN "Development environment setup completed successfully!"
echo
print_color $CYAN "📋 Summary:"
print_color $CYAN "- Configuration files have been created from examples"
print_color $CYAN "- Development tools have been checked and installed where possible"
print_color $CYAN "- Project dependencies have been restored"
print_color $CYAN "- Development guide created: $dev_config_file"
echo
print_color $YELLOW "🔧 Next Steps:"
print_color $YELLOW "1. Review and update configuration files (see $dev_config_file)"
print_color $YELLOW "2. Set up your database"
print_color $YELLOW "3. Configure external services"
print_color $YELLOW "4. Test the setup: dotnet run --project src/Jiro.Kernel/Jiro.App"
echo
print_color $GREEN "Happy coding! 🎉"
