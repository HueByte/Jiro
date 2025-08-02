#!/bin/bash

# Setup script for Jiro project with default and interactive modes

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Parse arguments
DEFAULT=false
NON_INTERACTIVE=false
DEV_MODE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --default)
            DEFAULT=true
            shift
            ;;
        --non-interactive)
            NON_INTERACTIVE=true
            shift
            ;;
        --dev)
            DEV_MODE=true
            shift
            ;;
        --help|-h)
            echo "Usage: $0 [--default|--non-interactive] [--dev]"
            echo ""
            echo "Options:"
            echo "  --default          Interactive setup - prompts for important values like API keys (recommended)"
            echo "  --non-interactive  Non-interactive setup - uses secure defaults, no prompts"
            echo "  --dev              Development mode - install dev tools and create development guide"
            echo "  --help, -h         Show this help message"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Validate arguments
if [ "$DEFAULT" = true ] && [ "$NON_INTERACTIVE" = true ]; then
    echo -e "${RED}Please specify either --default or --non-interactive, not both${NC}"
    exit 1
fi

if [ "$DEFAULT" = false ] && [ "$NON_INTERACTIVE" = false ] && [ "$DEV_MODE" = false ]; then
    echo -e "${RED}Please specify either --default, --non-interactive, or --dev mode${NC}"
    exit 1
fi

# If --dev is specified without --default or --non-interactive, default to --default
if [ "$DEV_MODE" = true ] && [ "$DEFAULT" = false ] && [ "$NON_INTERACTIVE" = false ]; then
    DEFAULT=true
    echo -e "${YELLOW}Development mode enabled - using interactive setup for configuration${NC}"
fi

if [ "$DEV_MODE" = true ]; then
    echo -e "${CYAN}=== Jiro Project Setup (Development Mode) ===${NC}"
else
    echo -e "${CYAN}=== Jiro Project Setup ===${NC}"
fi
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Function to generate secure random string
generate_secure_string() {
    local length=${1:-32}
    tr -dc 'A-Za-z0-9!@#$%^&*' < /dev/urandom | head -c "$length"
}

# Configuration variables
declare -A config
# Main API Key for this Jiro instance
config[ApiKey]=""

# Chat configuration
config[ChatAuthToken]=""
config[ChatSystemMessage]="I want you to act as personal assistant called Jiro. You are friendly, funny and sarcastic. You can ask me anything you want and engage in conversation."
config[ChatTokenLimit]=2000
config[ChatEnabled]="true"

# Note: JiroCloudApiKey will use the same value as ApiKey
config[JiroCloudGrpcServerUrl]="https://localhost:5001"
config[JiroCloudGrpcMaxRetries]=3
config[JiroCloudGrpcTimeoutMs]=30000
config[JiroCloudWebSocketHubUrl]="https://localhost:5001/instanceHub"
config[JiroCloudWebSocketHandshakeTimeoutMs]=15000
config[JiroCloudWebSocketKeepAliveIntervalMs]=15000
config[JiroCloudWebSocketReconnectionAttempts]=5
config[JiroCloudWebSocketReconnectionDelayMs]=5000
config[JiroCloudWebSocketServerTimeoutMs]=30000

# Setup based on mode
if [ "$DEFAULT" = true ]; then
    echo -e "${GREEN}Interactive setup mode (prompting for important values)${NC}"
    echo -e "${GRAY}Press Enter to use default value shown in [brackets]${NC}"
    echo ""
    
    # Main API Key
    read -p "Jiro API Key (for instance and cloud services) [auto-generated]: " api_key
    if [ -n "$api_key" ]; then
        config[ApiKey]="$api_key"
    else
        config[ApiKey]=$(generate_secure_string 32)
        echo -e "${GRAY}Generated: ${config[ApiKey]}${NC}"
    fi
    
    # Chat Configuration
    echo ""
    echo -e "${YELLOW}Chat Configuration:${NC}"
    read -p "OpenAI API Key (required for AI chat features): " chat_auth_token
    if [ -n "$chat_auth_token" ]; then
        config[ChatAuthToken]="$chat_auth_token"
    fi
    
    # JiroCloud Configuration
    echo ""
    echo -e "${YELLOW}JiroCloud Configuration:${NC}"
    
    read -p "JiroCloud gRPC Server URL [${config[JiroCloudGrpcServerUrl]}]: " grpc_server_url
    if [ -n "$grpc_server_url" ]; then
        config[JiroCloudGrpcServerUrl]="$grpc_server_url"
    fi
    
    read -p "JiroCloud WebSocket Hub URL [${config[JiroCloudWebSocketHubUrl]}]: " websocket_hub_url
    if [ -n "$websocket_hub_url" ]; then
        config[JiroCloudWebSocketHubUrl]="$websocket_hub_url"
    fi
    
elif [ "$NON_INTERACTIVE" = true ]; then
    echo -e "${YELLOW}Non-interactive setup mode (using secure defaults)${NC}"
    echo ""
    
    # Generate secure defaults for all values
    config[ApiKey]=$(generate_secure_string 32)
    
    echo -e "${GREEN}Generated secure default values for:${NC}"
    echo "  - Jiro API Key (for instance and cloud services)"
    echo ""
    echo -e "${YELLOW}⚠️  OpenAI API Key not set - you'll need to configure this manually for AI chat features${NC}"
fi

# Create .env file
echo ""
echo -e "${CYAN}Creating .env file...${NC}"

cat > "$PROJECT_ROOT/.env" << EOF
# Jiro Environment Configuration
# Generated by setup script on $(date '+%Y-%m-%d %H:%M:%S')

# ======================
# Docker Ports Configuration  
# ======================
JIRO_HTTP_PORT=8080
JIRO_HTTPS_PORT=8443
JIRO_ADDITIONAL_PORT=18090

# ======================
# MySQL Database Configuration
# ======================
DB_SERVER=mysql
MYSQL_DATABASE=jiro
MYSQL_USER=jiro
MYSQL_PASSWORD=your-secure-password-here
MYSQL_ROOT_PASSWORD=your-root-password-here
MYSQL_PORT=3306

# ======================
# Jiro Application Configuration (JIRO_ prefix)
# ======================
# Application API Configuration
JIRO_ApiKey=${config[ApiKey]}

# OpenAI Configuration (for AI features)
EOF

if [ -n "${config[ChatAuthToken]}" ]; then
    echo "OPENAI_API_KEY=${config[ChatAuthToken]}" >> "$PROJECT_ROOT/.env"
else
    echo "# OPENAI_API_KEY=sk-your-openai-api-key-here" >> "$PROJECT_ROOT/.env"
fi

echo "" >> "$PROJECT_ROOT/.env"
echo "# Chat Configuration" >> "$PROJECT_ROOT/.env"
if [ -n "${config[ChatAuthToken]}" ]; then
    echo "JIRO_Chat__AuthToken=${config[ChatAuthToken]}" >> "$PROJECT_ROOT/.env"
else
    echo "# JIRO_Chat__AuthToken=sk-your-openai-api-key-here" >> "$PROJECT_ROOT/.env"
fi

cat >> "$PROJECT_ROOT/.env" << EOF
JIRO_Chat__SystemMessage=${config[ChatSystemMessage]}
JIRO_Chat__TokenLimit=${config[ChatTokenLimit]}
JIRO_Chat__Enabled=${config[ChatEnabled]}

# JiroCloud Configuration
JIRO_JiroCloud__ApiKey=${config[ApiKey]}
JIRO_JiroCloud__Grpc__ServerUrl=${config[JiroCloudGrpcServerUrl]}
JIRO_JiroCloud__Grpc__MaxRetries=${config[JiroCloudGrpcMaxRetries]}
JIRO_JiroCloud__Grpc__TimeoutMs=${config[JiroCloudGrpcTimeoutMs]}
JIRO_JiroCloud__WebSocket__HubUrl=${config[JiroCloudWebSocketHubUrl]}
JIRO_JiroCloud__WebSocket__HandshakeTimeoutMs=${config[JiroCloudWebSocketHandshakeTimeoutMs]}
JIRO_JiroCloud__WebSocket__KeepAliveIntervalMs=${config[JiroCloudWebSocketKeepAliveIntervalMs]}
JIRO_JiroCloud__WebSocket__ReconnectionAttempts=${config[JiroCloudWebSocketReconnectionAttempts]}
JIRO_JiroCloud__WebSocket__ReconnectionDelayMs=${config[JiroCloudWebSocketReconnectionDelayMs]}
JIRO_JiroCloud__WebSocket__ServerTimeoutMs=${config[JiroCloudWebSocketServerTimeoutMs]}

# ======================
# Advanced JIRO_ Configuration Overrides
# ======================
# Custom Data Paths (optional - uncomment to customize)
# JIRO_DataPaths__Logs=Data/Logs
# JIRO_DataPaths__Messages=Data/Messages
# JIRO_DataPaths__Plugins=Data/Plugins
# JIRO_DataPaths__Themes=Data/Themes
EOF

# Create appsettings.json from example
echo -e "${CYAN}Creating appsettings.json...${NC}"

APPSETTINGS_EXAMPLE="$PROJECT_ROOT/src/Jiro.Kernel/Jiro.App/appsettings.example.json"
APPSETTINGS="$PROJECT_ROOT/src/Jiro.Kernel/Jiro.App/appsettings.json"

if [ -f "$APPSETTINGS_EXAMPLE" ]; then
    # Copy and update appsettings.json using jq if available, otherwise use sed
    if command -v jq &> /dev/null; then
        jq ".ApiKey = \"${config[ApiKey]}\" | 
            .Chat.AuthToken = \"${config[ChatAuthToken]:-your-openai-api-key}\" |
            .Chat.SystemMessage = \"${config[ChatSystemMessage]}\" |
            .Chat.TokenLimit = ${config[ChatTokenLimit]} |
            .Chat.Enabled = ${config[ChatEnabled]} |
            .JiroCloud.ApiKey = \"${config[ApiKey]}\" |
            .JiroCloud.Grpc.ServerUrl = \"${config[JiroCloudGrpcServerUrl]}\" |
            .JiroCloud.Grpc.MaxRetries = ${config[JiroCloudGrpcMaxRetries]} |
            .JiroCloud.Grpc.TimeoutMs = ${config[JiroCloudGrpcTimeoutMs]} |
            .JiroCloud.WebSocket.HubUrl = \"${config[JiroCloudWebSocketHubUrl]}\" |
            .JiroCloud.WebSocket.HandshakeTimeoutMs = ${config[JiroCloudWebSocketHandshakeTimeoutMs]} |
            .JiroCloud.WebSocket.KeepAliveIntervalMs = ${config[JiroCloudWebSocketKeepAliveIntervalMs]} |
            .JiroCloud.WebSocket.ReconnectionAttempts = ${config[JiroCloudWebSocketReconnectionAttempts]} |
            .JiroCloud.WebSocket.ReconnectionDelayMs = ${config[JiroCloudWebSocketReconnectionDelayMs]} |
            .JiroCloud.WebSocket.ServerTimeoutMs = ${config[JiroCloudWebSocketServerTimeoutMs]}" \
            "$APPSETTINGS_EXAMPLE" > "$APPSETTINGS"
    else
        # Fallback to sed-based approach
        cp "$APPSETTINGS_EXAMPLE" "$APPSETTINGS"
        
        # Update values using sed - escaping special characters in sed patterns
        ESCAPED_SYSTEM_MESSAGE=$(echo "${config[ChatSystemMessage]}" | sed 's/[[\.*^$()+?{|]/\\&/g')
        
        # Update main values
        sed -i.bak "s|\"ApiKey\": \".*\"|\"ApiKey\": \"${config[ApiKey]}\"|" "$APPSETTINGS"
        sed -i.bak "s|\"AuthToken\": \".*\"|\"AuthToken\": \"${config[ChatAuthToken]:-your-openai-api-key}\"|" "$APPSETTINGS"
        sed -i.bak "s|\"SystemMessage\": \".*\"|\"SystemMessage\": \"$ESCAPED_SYSTEM_MESSAGE\"|" "$APPSETTINGS"
        sed -i.bak "s|\"TokenLimit\": [0-9]*|\"TokenLimit\": ${config[ChatTokenLimit]}|" "$APPSETTINGS"
        sed -i.bak "s|\"Enabled\": [a-z]*|\"Enabled\": ${config[ChatEnabled]}|" "$APPSETTINGS"
        
        # Update JiroCloud values
        sed -i.bak "s|\"your-jirocloud-api-key-here\"|\"${config[ApiKey]}\"|" "$APPSETTINGS"
        sed -i.bak "s|\"ServerUrl\": \"https://localhost:5001\"|\"ServerUrl\": \"${config[JiroCloudGrpcServerUrl]}\"|" "$APPSETTINGS"
        sed -i.bak "s|\"MaxRetries\": [0-9]*|\"MaxRetries\": ${config[JiroCloudGrpcMaxRetries]}|" "$APPSETTINGS"
        sed -i.bak "s|\"TimeoutMs\": [0-9]*|\"TimeoutMs\": ${config[JiroCloudGrpcTimeoutMs]}|" "$APPSETTINGS"
        sed -i.bak "s|\"HubUrl\": \"https://localhost:5001/instanceHub\"|\"HubUrl\": \"${config[JiroCloudWebSocketHubUrl]}\"|" "$APPSETTINGS"
        sed -i.bak "s|\"HandshakeTimeoutMs\": [0-9]*|\"HandshakeTimeoutMs\": ${config[JiroCloudWebSocketHandshakeTimeoutMs]}|" "$APPSETTINGS"
        sed -i.bak "s|\"KeepAliveIntervalMs\": [0-9]*|\"KeepAliveIntervalMs\": ${config[JiroCloudWebSocketKeepAliveIntervalMs]}|" "$APPSETTINGS"
        sed -i.bak "s|\"ReconnectionAttempts\": [0-9]*|\"ReconnectionAttempts\": ${config[JiroCloudWebSocketReconnectionAttempts]}|" "$APPSETTINGS"
        sed -i.bak "s|\"ReconnectionDelayMs\": [0-9]*|\"ReconnectionDelayMs\": ${config[JiroCloudWebSocketReconnectionDelayMs]}|" "$APPSETTINGS"
        sed -i.bak "s|\"ServerTimeoutMs\": [0-9]*|\"ServerTimeoutMs\": ${config[JiroCloudWebSocketServerTimeoutMs]}|" "$APPSETTINGS"
        
        # Remove backup files
        rm -f "$APPSETTINGS.bak"
    fi
    
    echo -e "${GREEN}Created appsettings.json${NC}"
else
    echo -e "${YELLOW}⚠️  appsettings.example.json not found, skipping appsettings.json creation${NC}"
fi

# Restore dependencies
echo ""
echo -e "${CYAN}Restoring NuGet packages...${NC}"
cd "$PROJECT_ROOT"
dotnet restore src/Main.sln

# Build solution
echo ""
echo -e "${CYAN}Building solution...${NC}"
dotnet build src/Main.sln

# Development mode specific setup
if [ "$DEV_MODE" = true ]; then
    echo ""
    echo -e "${CYAN}Setting up development environment...${NC}"
    
    # Function to check if command exists
    check_command() {
        if command -v "$1" >/dev/null 2>&1; then
            return 0
        else
            return 1
        fi
    }
    
    # Install global .NET tools
    echo -e "${CYAN}Installing development tools...${NC}"
    
    # Install DocFX
    if check_command "docfx"; then
        echo -e "${GREEN}DocFX is already installed${NC}"
    else
        echo "Installing DocFX..."
        if dotnet tool install -g docfx --version 2.75.3 >/dev/null 2>&1; then
            echo -e "${GREEN}DocFX installed successfully${NC}"
        else
            echo -e "${YELLOW}Failed to install DocFX (may already be installed)${NC}"
        fi
    fi
    
    # Install markdownlint if Node.js is available
    if check_command "node"; then
        if check_command "markdownlint"; then
            echo -e "${GREEN}markdownlint is already installed${NC}"
        else
            echo "Installing markdownlint-cli..."
            if npm install -g markdownlint-cli >/dev/null 2>&1; then
                echo -e "${GREEN}markdownlint-cli installed successfully${NC}"
            else
                echo -e "${YELLOW}Failed to install markdownlint-cli${NC}"
            fi
        fi
    else
        echo -e "${YELLOW}Node.js not available, skipping markdownlint installation${NC}"
    fi
    
    # Create development guide
    echo -e "${CYAN}Creating development guide...${NC}"
    
    cat > "$PROJECT_ROOT/DEV-SETUP.md" << 'DEVEOF'
# Jiro AI Assistant - Development Configuration

This file contains all the configuration values you need to set manually for development.
After running setup with --dev flag, please review and update these settings.

## Configuration Files Created:
- .env (Docker environment)
- appsettings.json (Application settings)

## Environment Variables (.env)
Location: .env

Generated settings (review and update as needed):
- Docker ports and MySQL configuration
- Jiro API key (auto-generated)
- JiroCloud configuration

## Application Settings (appsettings.json)
Location: src/Jiro.Kernel/Jiro.App/appsettings.json

Required settings to update:
- Chat.AuthToken: Add your OpenAI API key for AI features
- ConnectionStrings.JiroContext: Database connection (auto-configured for SQLite)

## Development Tools Installed:
- DocFX (documentation generation)
- markdownlint-cli (markdown linting, if Node.js available)

## Next Steps:
1. Review and update the configuration files listed above
2. Configure OpenAI API key for chat features
3. Run database migrations: dotnet ef database update -p src/Jiro.Kernel/Jiro.Infrastructure -s src/Jiro.Kernel/Jiro.App
4. Start the application: cd src/Jiro.Kernel/Jiro.App && dotnet run
5. Run tests: dotnet test src/Main.sln

## Useful Development Commands:
- Build project: dotnet build src/Main.sln
- Run tests: dotnet test src/Main.sln
- Format code: dotnet format src/Main.sln
- Generate docs: ./scripts/docfx-gen.sh (if available)
- Lint markdown: ./scripts/markdown-lint.sh (if available)
- Local CI test: ./scripts/local-ci-test.sh (if available)

For more information, see the project documentation.
DEVEOF
    
    echo -e "${GREEN}Development guide created: DEV-SETUP.md${NC}"
fi

echo ""
echo -e "${GREEN}=== Setup Complete ===${NC}"
echo ""
echo -e "${CYAN}Configuration files created:${NC}"
echo "  - .env (Docker environment)"
echo "  - appsettings.json (Application settings)"
echo ""

if [ -z "${config[ChatAuthToken]}" ]; then
    echo ""
    echo -e "${YELLOW}IMPORTANT: Configuration items to set manually:${NC}"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo -e "  • ${RED}OpenAI API Key: Required for AI chat features${NC}"
    echo "    - Update 'JIRO_Chat__AuthToken' in .env file"
    echo "    - Update 'Chat.AuthToken' in appsettings.json"
    echo ""
    echo "  Generated secure values:"
    echo "  • Jiro API Key: ${config[ApiKey]}"
    echo ""
    echo "  Service URLs:"
    echo "  • JiroCloud gRPC: ${config[JiroCloudGrpcServerUrl]}"
    echo "  • JiroCloud WebSocket: ${config[JiroCloudWebSocketHubUrl]}"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
fi

echo -e "${YELLOW}Next steps:${NC}"
echo "1. Review the generated configuration files (.env and appsettings.json)"
if [ -z "${config[ChatAuthToken]}" ]; then
    echo "2. Configure OpenAI API Key (see above) for AI chat features"
    echo "3. Run database migrations: dotnet ef database update -p src/Jiro.Kernel/Jiro.Infrastructure -s src/Jiro.Kernel/Jiro.App"
    echo "4. Start the application:"
else
    echo "2. Run database migrations: dotnet ef database update -p src/Jiro.Kernel/Jiro.Infrastructure -s src/Jiro.Kernel/Jiro.App"
    echo "3. Start the application:"
fi
echo "   - Direct: cd src/Jiro.Kernel/Jiro.App && dotnet run"

if [ "$DEV_MODE" = true ]; then
    echo ""
    echo -e "${CYAN}Development mode completed:${NC}"
    echo "📋 Development tools installed and configured"
    echo "📖 Development guide created: DEV-SETUP.md"
    echo "🔧 Review DEV-SETUP.md for detailed configuration instructions"
fi
echo ""