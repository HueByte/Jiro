#!/bin/bash

# Local CI/CD Testing Script for Jiro.Kernel
# This script mimics the GitHub Actions workflows for local testing

set -e

# Parse command line arguments
SKIP_DOCKER=false
SKIP_SECURITY=false
SKIP_PERFORMANCE=false
CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-docker)
            SKIP_DOCKER=true
            shift
            ;;
        --skip-security)
            SKIP_SECURITY=true
            shift
            ;;
        --skip-performance)
            SKIP_PERFORMANCE=true
            shift
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --skip-docker       Skip Docker build and tests"
            echo "  --skip-security     Skip security checks"
            echo "  --skip-performance  Skip performance tests"
            echo "  --configuration     Build configuration (Debug|Release, default: Release)"
            echo "  -h, --help         Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
# Get script directory and navigate to project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_ROOT"

SOLUTION_PATH="./src/Main.sln"
DOCKERFILE_PATH="./src/Jiro.Kernel/Dockerfile"
DOCKER_IMAGE_NAME="jiro-kernel-test"
PROJECT_PATH="./src/Jiro.Kernel"

# Helper functions
print_header() {
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check prerequisites
check_prerequisites() {
    print_header "Checking Prerequisites"
    
    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK not found. Please install .NET 10 SDK."
        exit 1
    fi
    
    DOTNET_VERSION=$(dotnet --version)
    print_success ".NET SDK version: $DOTNET_VERSION"
    
    # Prepare configuration files
    if [ ! -f "./src/Jiro.Kernel/Jiro.App/Configuration/appsettings.json" ]; then
        if [ -f "./src/Jiro.Kernel/Jiro.App/Configuration/appsettings.example.json" ]; then
            cp "./src/Jiro.Kernel/Jiro.App/Configuration/appsettings.example.json" "./src/Jiro.Kernel/Jiro.App/Configuration/appsettings.json"
            print_success "Copied Configuration/appsettings.example.json to Configuration/appsettings.json"
        else
            print_warning "No Configuration/appsettings.example.json found"
        fi
    fi
}

# Build and test
build_and_test() {
    print_header "Build and Test"
    
    echo "Restoring dependencies..."
    dotnet restore "$SOLUTION_PATH"
    print_success "Dependencies restored"
    
    echo "Building solution..."
    dotnet build "$SOLUTION_PATH" --no-restore --configuration "$CONFIGURATION"
    print_success "Build completed"
    
    echo "Checking code formatting..."
    if dotnet format "$SOLUTION_PATH" --no-restore --verify-no-changes --verbosity diagnostic; then
        print_success "Code formatting is correct"
    else
        print_warning "Code formatting issues found. Run 'dotnet format' to fix them."
    fi
    
    echo "Running tests..."
    dotnet test "$SOLUTION_PATH" --no-build --configuration "$CONFIGURATION" --logger "console;verbosity=normal"
    print_success "Tests completed"
}

# Security checks
security_checks() {
    if [ "$SKIP_SECURITY" = true ]; then
        print_warning "Skipping security checks"
        return
    fi
    
    print_header "Security Checks"
    
    echo "Checking for vulnerable packages..."
    if dotnet list "$SOLUTION_PATH" package --vulnerable --include-transitive; then
        print_success "No vulnerable packages found"
    else
        print_warning "Vulnerable packages detected. Please review and update."
    fi
    
    # Install security scanner if available
    if command -v safety &> /dev/null; then
        echo "Running safety check..."
        safety check
    else
        print_warning "Safety scanner not installed. Install with 'pip install safety'"
    fi
}

# Docker build and test
docker_build() {
    if [ "$SKIP_DOCKER" = true ]; then
        print_warning "Skipping Docker tests (--skip-docker flag)"
        return
    fi
    
    # Check if Docker is available
    if ! command -v docker &> /dev/null; then
        print_warning "Skipping Docker tests (Docker not available)"
        return
    fi
    
    print_header "Docker Build and Test"
    
    echo "Building Docker image..."
    docker build -t "$DOCKER_IMAGE_NAME" -f "$DOCKERFILE_PATH" "$PROJECT_PATH"
    print_success "Docker image built successfully"
    
    echo "Testing Docker image..."
    # Start container in background
    CONTAINER_ID=$(docker run -d --name jiro-kernel-test "$DOCKER_IMAGE_NAME")
    
    # Wait a moment for startup
    sleep 5
    
    # Check if container is running
    if docker ps | grep -q jiro-kernel-test; then
        print_success "Container started successfully"
        
        # Check logs for any immediate errors
        echo "Container logs:"
        docker logs jiro-kernel-test
        
        # Stop and remove container
        docker stop jiro-kernel-test > /dev/null
        docker rm jiro-kernel-test > /dev/null
        print_success "Container test completed"
    else
        print_error "Container failed to start"
        docker logs jiro-kernel-test || true
        docker rm jiro-kernel-test > /dev/null 2>&1 || true
        exit 1
    fi
    
    # Clean up image
    read -p "Remove test Docker image? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker rmi "$DOCKER_IMAGE_NAME" > /dev/null
        print_success "Test image removed"
    fi
}

# Documentation tests
documentation_tests() {
    print_header "Documentation Tests"
    
    echo "Testing project structure generation..."
    if ./scripts/generate-project-structure.sh "temp-project-structure.md"; then
        if [ -f "temp-project-structure.md" ]; then
            print_success "Project structure generation test passed"
            rm -f "temp-project-structure.md"
        else
            print_warning "Project structure generation did not create output file"
        fi
    else
        print_warning "Project structure generation test failed"
    fi
    
    echo "Checking for DocFX configuration..."
    if [ -f "./dev/docfx.json" ]; then
        print_success "DocFX configuration found"
        
        # Test DocFX build if available
        if command -v docfx &> /dev/null; then
            echo "Testing DocFX build..."
            cd "./src" || exit 1
            if docfx docfx.json; then
                print_success "DocFX configuration is valid"
            else
                print_warning "DocFX build test failed"
            fi
            cd - > /dev/null || exit 1
        else
            print_warning "DocFX not installed. Install with: dotnet tool install -g docfx"
        fi
    else
        print_warning "DocFX configuration not found at ./dev/docfx.json"
    fi
}

# Performance tests
performance_tests() {
    if [ "$SKIP_PERFORMANCE" = true ]; then
        print_warning "Skipping performance tests"
        return
    fi
    
    print_header "Performance Tests"
    
    echo "Looking for performance tests..."
    PERF_TESTS=$(dotnet test "$SOLUTION_PATH" --list-tests | grep -i performance || true)
    
    if [ -n "$PERF_TESTS" ]; then
        echo "Running performance tests..."
        dotnet test "$SOLUTION_PATH" --no-build --configuration "$CONFIGURATION" --filter "Category=Performance"
        print_success "Performance tests completed"
    else
        print_warning "No performance tests found. Consider adding tests with [Category(\"Performance\")] attribute."
    fi
    
    # Check for benchmark projects
    BENCHMARK_PROJECTS=$(find "$PROJECT_PATH" -name "*.csproj" -exec grep -l "BenchmarkDotNet" {} \; 2>/dev/null || true)
    
    if [ -n "$BENCHMARK_PROJECTS" ]; then
        echo "Found benchmark projects. Running benchmarks..."
        for project in $BENCHMARK_PROJECTS; do
            echo "Running benchmarks in $project"
            dotnet run --project "$project" --configuration "$CONFIGURATION" --framework net10.0 -- --filter "*" || true
        done
        print_success "Benchmarks completed"
    else
        print_warning "No benchmark projects found. Consider adding BenchmarkDotNet for performance testing."
    fi
}

# Generate report
generate_report() {
    print_header "Test Summary"
    
    echo "Local CI/CD test completed successfully!"
    echo ""
    echo "Next steps:"
    echo "1. Review any warnings above"
    echo "2. Fix code formatting issues if any"
    echo "3. Update vulnerable dependencies if any"
    echo "4. Commit and push to trigger GitHub Actions"
    echo ""
    echo "GitHub Actions workflows:"
    echo "- jiro-kernel-ci.yml: Main CI/CD pipeline"
    echo "- jiro-kernel-security.yml: Weekly security scans"
    echo "- jiro-kernel-performance.yml: Performance testing"
    echo ""
    print_success "All local tests completed!"
}

# Main execution
main() {
    echo -e "${BLUE}Jiro.Kernel Local CI/CD Test Runner${NC}"
    echo "This script runs the same checks as GitHub Actions"
    echo ""
    
    check_prerequisites
    build_and_test
    security_checks
    documentation_tests
    docker_build
    performance_tests
    generate_report
}

# Run main function
main "$@"
