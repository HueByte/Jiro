#!/bin/bash

# DocFX Runner for Jiro AI Assistant
# Builds documentation using DocFX

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
ACTION="build"
CONFIG_PATH="dev/docfx.json"
PORT=8080
FORCE_CLEAN=false
VERBOSE_MODE=false

# Functions
print_color() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_error() {
    print_color $RED "$1"
}

print_success() {
    print_color $GREEN "$1"
}

print_warning() {
    print_color $YELLOW "$1"
}

print_info() {
    print_color $CYAN "$1"
}

check_command() {
    if command -v "$1" >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

show_help() {
    cat << EOF
DocFX Runner for Jiro AI Assistant

USAGE:
    ./docfx-gen.sh [OPTIONS] [ACTION]

ACTIONS:
    build       Build documentation (default)
    serve       Build and serve documentation
    clean       Clean generated files
    init        Initialize new DocFX project

OPTIONS:
    -c, --config PATH   Path to DocFX configuration file (default: dev/docfx.json)
    -p, --port PORT     Port for serving documentation (default: 8080)
    -f, --force         Force rebuild by cleaning first
    -v, --verbose       Show verbose output during build
    -h, --help          Show this help message

EXAMPLES:
    ./docfx-gen.sh                          # Build documentation
    ./docfx-gen.sh serve                    # Build and serve on default port
    ./docfx-gen.sh serve --port 3000        # Build and serve on port 3000
    ./docfx-gen.sh clean                    # Clean generated files
    ./docfx-gen.sh build --force --verbose  # Force rebuild with verbose output

REQUIREMENTS:
    - .NET SDK must be installed
    - DocFX will be installed automatically if missing

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        build|serve|clean|init)
            ACTION="$1"
            shift
            ;;
        -c|--config)
            CONFIG_PATH="$2"
            shift 2
            ;;
        -p|--port)
            PORT="$2"
            shift 2
            ;;
        -f|--force)
            FORCE_CLEAN=true
            shift
            ;;
        -v|--verbose)
            VERBOSE_MODE=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        -*)
            print_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
        *)
            print_error "Unknown argument: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Change to repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$REPO_ROOT"

print_info "📚 Jiro AI Assistant DocFX Runner"
print_info "======================="
echo

# Check if .NET is installed
if ! check_command dotnet; then
    print_error "❌ .NET is not installed or not in PATH"
    print_warning "Please install .NET from https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_success "✅ .NET version: $DOTNET_VERSION"

# Check if DocFX is installed
if ! check_command docfx; then
    print_warning "⚠️  DocFX not found, installing globally..."
    if dotnet tool install -g docfx --version 2.78.3; then
        print_success "✅ DocFX installed successfully"
    else
        print_error "❌ Failed to install DocFX"
        print_warning "You may need to run with sudo or check your .NET installation"
        exit 1
    fi
fi

DOCFX_VERSION=$(docfx --version)
print_success "✅ DocFX version: $DOCFX_VERSION"
echo

# Check for DocFX configuration
if [[ ! -f "$CONFIG_PATH" ]]; then
    print_error "❌ DocFX configuration file not found: $CONFIG_PATH"
    print_warning "Please ensure the configuration file exists."
    exit 1
fi

print_info "📋 Using configuration: $CONFIG_PATH"
print_info "🎯 Action: $ACTION"

# Handle different actions
case $ACTION in
    "clean")
        print_info "🧹 Cleaning generated files..."
        
        # Remove generated directory
        GENERATED_PATH="$REPO_ROOT/src/_site"
        if [[ -d "$GENERATED_PATH" ]]; then
            rm -rf "$GENERATED_PATH"
            print_success "✅ Removed _site directory"
        fi
        
        # Remove API docs if they exist
        CONFIG_DIR="$(dirname "$CONFIG_PATH")"
        API_PATH="$REPO_ROOT/$CONFIG_DIR/api"
        if [[ -d "$API_PATH" ]]; then
            rm -rf "$API_PATH"
            print_success "✅ Removed API documentation"
        fi
        
        print_success "🎉 Cleanup completed!"
        exit 0
        ;;
        
    "init")
        print_info "🚀 Initializing DocFX project..."
        read -p "Enter directory name for new DocFX project (default: docfx-init): " INIT_DIR
        if [[ -z "$INIT_DIR" ]]; then
            INIT_DIR="docfx-init"
        fi
        
        docfx init -q -o "$INIT_DIR"
        print_success "✅ DocFX project initialized in $INIT_DIR"
        exit 0
        ;;
        
    "build")
        # Force clean if requested
        if [[ "$FORCE_CLEAN" == true ]]; then
            print_info "🧹 Force cleaning before build..."
            "$0" clean --config "$CONFIG_PATH"
            echo
        fi
        
        # Generate project structure documentation
        print_info "🏗️ Generating project structure documentation..."
        STRUCTURE_SCRIPT="$SCRIPT_DIR/generate-project-structure.sh"
        if [[ -f "$STRUCTURE_SCRIPT" ]]; then
            if bash "$STRUCTURE_SCRIPT" "src/docs/project-structure.md"; then
                print_success "✅ Project structure documentation generated"
            else
                print_warning "⚠️  Failed to generate project structure"
                print_info "Continuing with documentation build..."
            fi
        else
            print_warning "⚠️  Project structure script not found, skipping..."
        fi
        echo
        
        print_info "🔨 Building documentation..."
        
        DOCFX_ARGS=("$CONFIG_PATH")
        if [[ "$VERBOSE_MODE" == true ]]; then
            DOCFX_ARGS+=(--verbose)
        fi
        
        if docfx "${DOCFX_ARGS[@]}"; then
            print_success "✅ Documentation built successfully!"
            
            # Show output location
            GENERATED_PATH="$REPO_ROOT/src/_site"
            if [[ -d "$GENERATED_PATH" ]]; then
                print_info "📁 Output location: $GENERATED_PATH"
                INDEX_PATH="$GENERATED_PATH/index.html"
                if [[ -f "$INDEX_PATH" ]]; then
                    print_info "🌐 Open: file://$INDEX_PATH"
                fi
            fi
        else
            print_error "❌ Documentation build failed"
            exit 1
        fi
        ;;
        
    "serve")
        # Generate project structure documentation
        print_info "🏗️ Generating project structure documentation..."
        STRUCTURE_SCRIPT="$SCRIPT_DIR/generate-project-structure.sh"
        if [[ -f "$STRUCTURE_SCRIPT" ]]; then
            if bash "$STRUCTURE_SCRIPT" "dev/docs/project-structure.md"; then
                print_success "✅ Project structure documentation generated"
            else
                print_warning "⚠️  Failed to generate project structure"
                print_info "Continuing with documentation build..."
            fi
        else
            print_warning "⚠️  Project structure script not found, skipping..."
        fi
        echo
        
        # Build first
        print_info "🔨 Building documentation before serving..."
        
        BUILD_ARGS=("$CONFIG_PATH")
        if [[ "$VERBOSE_MODE" == true ]]; then
            BUILD_ARGS+=(--verbose)
        fi
        
        if ! docfx "${BUILD_ARGS[@]}"; then
            print_error "❌ Build failed, cannot serve"
            exit 1
        fi
        
        print_info "🚀 Starting documentation server on port $PORT..."
        print_success "🌐 Documentation will be available at: http://localhost:$PORT"
        print_info "⏹️  Press Ctrl+C to stop the server"
        echo
        
        docfx "$CONFIG_PATH" --serve --port "$PORT"
        ;;
        
    *)
        print_error "❌ Unknown action: $ACTION"
        show_help
        exit 1
        ;;
esac

echo
print_success "🎉 DocFX operation completed successfully!"

if [[ "$ACTION" == "build" ]]; then
    print_info "💡 Use 'serve' action to build and serve the documentation locally"
    print_info "💡 Use 'clean' action to remove generated files"
fi