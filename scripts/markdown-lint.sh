#!/bin/bash

# Markdownlint Runner for Jiro AI Assistant
# Runs markdownlint on all Markdown files in the repository

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
FIX_MODE=false
VERBOSE_MODE=false
LINT_PATH="**/*.md"

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
Markdownlint Runner for Jiro AI Assistant

USAGE:
    ./markdownlint.sh [OPTIONS] [PATH]

OPTIONS:
    -f, --fix       Automatically fix issues that can be fixed
    -v, --verbose   Show verbose output during linting
    -h, --help      Show this help message

ARGUMENTS:
    PATH            Specific path or file to lint (defaults to **/*.md)

EXAMPLES:
    ./markdownlint.sh                    # Lint all Markdown files
    ./markdownlint.sh --fix              # Lint and auto-fix issues
    ./markdownlint.sh docs/*.md          # Lint only docs directory
    ./markdownlint.sh -v --fix           # Verbose mode with auto-fix

REQUIREMENTS:
    - Node.js and npm must be installed
    - markdownlint-cli will be installed automatically if missing

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -f|--fix)
            FIX_MODE=true
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
            LINT_PATH="$1"
            shift
            ;;
    esac
done

# Change to repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$REPO_ROOT"

print_info "🔍 Jiro AI Assistant Markdownlint Runner"
print_info "=============================="
echo

# Check if Node.js is installed
if ! check_command node; then
    print_error "❌ Node.js is not installed or not in PATH"
    print_warning "Please install Node.js from https://nodejs.org/"
    exit 1
fi

NODE_VERSION=$(node --version)
print_success "✅ Node.js version: $NODE_VERSION"

# Check if npm is installed
if ! check_command npm; then
    print_error "❌ npm is not installed or not in PATH"
    print_warning "Please install npm or use a Node.js installer that includes npm"
    exit 1
fi

# Check if markdownlint-cli is installed
if ! check_command markdownlint; then
    print_warning "⚠️  markdownlint-cli not found, installing globally..."
    if npm install -g markdownlint-cli; then
        print_success "✅ markdownlint-cli installed successfully"
    else
        print_error "❌ Failed to install markdownlint-cli"
        print_warning "You may need to run with sudo or use a Node version manager"
        exit 1
    fi
fi

MARKDOWNLINT_VERSION=$(markdownlint --version)
print_success "✅ markdownlint-cli version: $MARKDOWNLINT_VERSION"
echo

# Check for markdownlint configuration
CONFIG_FILE=""
if [[ -f "dev/config/.markdownlint.json" ]]; then
    print_info "📋 Using configuration from dev/config/.markdownlint.json"
    CONFIG_FILE="--config dev/config/.markdownlint.json"
elif [[ -f ".markdownlint.json" ]]; then
    print_info "📋 Using configuration from .markdownlint.json (root location)"
    CONFIG_FILE="--config .markdownlint.json"
else
    print_warning "⚠️  No .markdownlint.json found, using default rules"
fi

# Build markdownlint command arguments
MARKDOWNLINT_ARGS=(
    "$LINT_PATH"
    --ignore "node_modules"
    --ignore "TestResults"
    --ignore "dev/_site"
    --ignore "dev/_temp"
    --ignore "dev/api"
    --ignore "_site"
    --ignore "bin"
    --ignore "obj"
    --ignore "dev-local"
)

# Add configuration file if present
if [[ -n "$CONFIG_FILE" ]]; then
    MARKDOWNLINT_ARGS+=($CONFIG_FILE)
fi

# Add fix flag if requested
if [[ "$FIX_MODE" == true ]]; then
    MARKDOWNLINT_ARGS+=(--fix)
    print_info "🔧 Auto-fix mode enabled"
fi

print_info "🚀 Running markdownlint..."
print_info "Command: markdownlint ${MARKDOWNLINT_ARGS[*]}"
echo

# Run markdownlint
if [[ "$VERBOSE_MODE" == true ]]; then
    # Run with verbose output
    if markdownlint "${MARKDOWNLINT_ARGS[@]}"; then
        LINT_SUCCESS=true
    else
        LINT_SUCCESS=false
    fi
else
    # Capture output and only show if there are errors
    if OUTPUT=$(markdownlint "${MARKDOWNLINT_ARGS[@]}" 2>&1); then
        LINT_SUCCESS=true
        if [[ -n "$OUTPUT" ]]; then
            print_info "Output:"
            echo "$OUTPUT"
        fi
    else
        LINT_SUCCESS=false
        print_error "❌ Markdownlint found issues:"
        echo "$OUTPUT"
    fi
fi

echo
if [[ "$LINT_SUCCESS" == true ]]; then
    if [[ "$FIX_MODE" == true ]]; then
        print_success "🎉 Markdownlint completed with auto-fix!"
        print_info "📝 Check the changes and commit if appropriate."
    else
        print_success "🎉 Markdownlint completed successfully!"
        print_info "💡 Use --fix parameter to automatically fix issues."
    fi
    exit 0
else
    print_error "❌ Markdownlint completed with errors."
    if [[ "$FIX_MODE" == false ]]; then
        print_info "💡 Use --fix parameter to automatically fix issues that can be auto-fixed."
    fi
    exit 1
fi