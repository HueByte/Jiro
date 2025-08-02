<p align="center">
    <img src="assets/JiroBanner.png" style="border-radius: 15px;" alt="Jiro AI Assistant Banner"/>
</p>

<h1 align="center">🤖 Jiro AI Assistant</h1>

<p align="center">
    <strong>Your intelligent companion powered by AI and extensible plugins!</strong>
</p>

<p align="center">
    <a href="https://github.com/HueByte/Jiro/actions/workflows/create-release.yml">
        <img src="https://img.shields.io/github/actions/workflow/status/HueByte/Jiro/create-release.yml?branch=main&style=for-the-badge&label=build" alt="Build Status"/>
    </a>
    <a href="https://github.com/HueByte/Jiro/releases/latest">
        <img src="https://img.shields.io/github/v/release/HueByte/Jiro?style=for-the-badge&color=blue" alt="Latest Release"/>
    </a>
    <a href="https://github.com/HueByte/Jiro/commits/main">
        <img src="https://img.shields.io/github/last-commit/HueByte/Jiro?style=for-the-badge&color=orange" alt="Last Commit"/>
    </a>
    <a href="https://github.com/HueByte/Jiro/stargazers">
        <img src="https://img.shields.io/github/stars/HueByte/Jiro?style=for-the-badge&color=yellow" alt="GitHub Stars"/>
    </a>
    <a href="https://github.com/HueByte/Jiro/issues">
        <img src="https://img.shields.io/github/issues/HueByte/Jiro?style=for-the-badge&color=red" alt="GitHub Issues"/>
    </a>
    <a href="https://github.com/HueByte/Jiro/blob/main/LICENSE">
        <img src="https://img.shields.io/github/license/HueByte/Jiro?style=for-the-badge&color=green" alt="License"/>
    </a>
    <a href="https://dotnet.microsoft.com/download">
        <img src="https://img.shields.io/badge/.NET-9.0-purple?style=for-the-badge" alt=".NET 9.0"/>
    </a>
    <a href="https://github.com/HueByte/Jiro">
        <img src="https://img.shields.io/github/languages/code-size/HueByte/Jiro?style=for-the-badge&color=purple" alt="Code Size"/>
    </a>
</p>

---

## 🌟 What is Jiro?

Meet **Jiro** – your personal AI assistant that combines the power of ChatGPT with a robust, extensible plugin system! Whether you need help with daily tasks, want to check the weather, manage conversations, or build custom integrations, Jiro is here to make your life easier and more productive.

## ✨ What Makes Jiro Special?

🧠 **AI-Powered Conversations** - Leverage ChatGPT's intelligence for natural, context-aware interactions  
🔌 **Plugin Architecture** - Extend functionality with custom commands and integrations  
🌤️ **Built-in Weather** - Get real-time weather updates and forecasts  
💬 **Session Management** - Maintain conversation context across multiple interactions  
🛡️ **Secure & Private** - Your data stays secure with robust authentication  
🚀 **Modern Tech Stack** - Built with .NET 9, React, and modern web technologies  

## 🎮 Quick Demo

```bash
# Chat with Jiro
You: "What's the weather like today?"
Jiro: "🌤️ It's partly cloudy and 72°F in your area. Perfect for a walk!"

# Get help with commands
You: "/help weather"
Jiro: "Here are the weather commands I can help you with..."

# Custom plugins in action
You: "/net ping google.com"
Jiro: "🌐 Pinging google.com... Response time: 23ms ✅"
```

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for the web interface)
- [Python 3.8+](https://python.org/) (for tokenizer service)
- OpenAI API Key (optional, for AI chat features)

### 🏃‍♂️ Get Running in 2 Minutes (Automated Setup)

1. **Clone the repository**

   ```bash
   git clone https://github.com/HueByte/Jiro.git
   cd Jiro
   ```

2. **Run the setup script** ⚡

   ```bash
   # Interactive setup (recommended) - prompts for API keys
   ./scripts/setup-project.ps1 -Default         # Windows
   ./scripts/setup-project.sh --default         # Linux/macOS
   
   # OR Non-interactive setup - secure defaults only
   ./scripts/setup-project.ps1 -NonInteractive  # Windows
   ./scripts/setup-project.sh --non-interactive # Linux/macOS
   ```

3. **Run database migrations**

   ```bash
   dotnet ef database update -p src/Jiro.Kernel/Jiro.Infrastructure -s src/Jiro.Kernel/Jiro.App
   ```

4. **Start Jiro**

   ```bash
   # Option 1: Docker (includes database)
   docker-compose up -d
   
   # Option 2: Direct run
   cd src/Jiro.Kernel/Jiro.App
   dotnet run
   ```

5. **Start the tokenizer** (in a new terminal)

   ```bash
   cd src/Jiro.TokenApi
   pip install -r requirements.txt
   python main.py
   ```

6. **Open your browser** and navigate to `https://localhost:5001` 🎉

### 🔧 Manual Setup (Alternative)

If you prefer manual configuration:

<details>
<summary>Click to expand manual setup steps</summary>

1. **Clone and navigate**

   ```bash
   git clone https://github.com/HueByte/Jiro.git
   cd Jiro
   ```

2. **Set up configuration files**

   ```bash
   # Copy environment template for Docker
   cp .env.example .env
   
   # Copy application settings
   cd src/Jiro.Kernel/Jiro.App
   cp appsettings.example.json appsettings.json
   
   # Copy client app settings
   cd clientapp
   cp envExamples/.env.example .env
   cp envExamples/.env.development.example .env.development
   ```

3. **Configure your settings**

   Edit `.env` file:

   ```bash
   # Required for AI features
   OPENAI_API_KEY=sk-your-openai-api-key
   
   # Required for authentication
   JWT_SECRET=your-jwt-secret-minimum-32-characters
   
   # Database configuration
   MYSQL_ROOT_PASSWORD=your-root-password
   MYSQL_PASSWORD=your-mysql-password
   ```

   Edit `appsettings.json`:

   ```json
   {
     "Gpt": {
       "AuthToken": "your-openai-api-key-here"
     },
     "JWT": {
       "Secret": "your-jwt-secret-minimum-32-characters"
     }
   }
   ```

4. **Build and run**

   ```bash
   cd ../  # Back to Jiro.App directory
   dotnet restore src/Main.sln
   dotnet build src/Main.sln
   dotnet run
   ```

</details>

## 🔌 Plugin Development

Want to extend Jiro with your own commands? It's easier than you think!

```csharp
[Command("hello")]
public class HelloCommand : BaseCommand
{
    public override async Task<string> ExecuteAsync(string[] args)
    {
        var name = args.Length > 0 ? args[0] : "World";
        return $"Hello, {name}! 👋";
    }
}
```

📚 **Learn More**: This project demonstrates plugin architecture patterns and extensible command system design

## 🏗️ Architecture Overview

Jiro is built as a modular, self-contained AI assistant that can run locally while optionally connecting to external services. The architecture follows clean separation of concerns with a focus on extensibility and performance:

```mermaid
graph TB
    subgraph "🌐 External Services"
        JiroCloud[🚀 JiroCloud<br/>Optional Remote Commands]
        OpenAI[🧠 OpenAI API<br/>Language Models]
        WeatherAPI[🌤️ Weather Service<br/>Location & Forecast]
    end

    subgraph "💻 Local Jiro Instance"
        subgraph "🎯 Client Layer"
            PythonCLI[🐍 Python CLI<br/>jiro.py]
            WebClient[🌐 Web Interface<br/>Future Feature]
        end

        subgraph "🚀 Jiro.App Layer"
            JiroApp[🎮 Jiro.App<br/>Main Host Application]
            RestAPI[🌐 REST API<br/>:18090]
            GrpcService[📡 gRPC Service<br/>JiroCloud Integration]
        end

        subgraph "💼 Jiro.Core Layer"
            ChatService[💬 Chat Service<br/>Conversation Management]
            CmdSystem[⚡ Command System<br/>Plugin Framework]
            WeatherService[🌤️ Weather Service<br/>Location & Forecasts]
            PersonaService[👤 Persona Service<br/>AI Personality]
            ConversationService[💬 Conversation Service<br/>Context Management]
        end

        subgraph "🗄️ Jiro.Infrastructure Layer"
            EFCore[🗃️ Entity Framework<br/>Data Access]
            Repositories[📊 Repositories<br/>Data Operations]
            Cache[⚡ Memory Cache<br/>Performance]
        end

        subgraph "💾 Storage"
            SQLite[💾 SQLite<br/>Default Database]
            MySQL[🗄️ MySQL<br/>Docker Option]
        end
    end

    subgraph "🔌 Plugin Ecosystem"
        BaseCommands[⚡ Base Commands<br/>Built-in Features]
        CustomPlugins[🔧 Custom Plugins<br/>Extensible via NuGet]
    end

    %% Client to Application Flow
    PythonCLI -->|HTTP Requests| RestAPI
    WebClient -->|HTTP Requests| RestAPI
    RestAPI --> JiroApp

    %% External Service Integration
    JiroApp -.->|Optional| GrpcService
    GrpcService -.->|Commands| JiroCloud
    ChatService -->|AI Requests| OpenAI
    WeatherService -->|API Calls| WeatherAPI

    %% Core Service Interactions
    JiroApp --> ChatService
    JiroApp --> CmdSystem
    JiroApp --> WeatherService
    JiroApp --> PersonaService
    JiroApp --> ConversationService

    %% Plugin System
    CmdSystem --> BaseCommands
    CmdSystem --> CustomPlugins

    %% Data Layer
    ChatService --> EFCore
    WeatherService --> EFCore
    PersonaService --> EFCore
    ConversationService --> EFCore
    EFCore --> Repositories
    Repositories --> Cache
    EFCore --> SQLite
    EFCore -.->|Docker| MySQL

    %% Styling
    classDef external fill:#FFE6CC,stroke:#D79B00,stroke-width:2px
    classDef client fill:#E1F5FE,stroke:#0277BD,stroke-width:2px
    classDef app fill:#F3E5F5,stroke:#7B1FA2,stroke-width:2px
    classDef core fill:#E8F5E8,stroke:#2E7D32,stroke-width:2px
    classDef infra fill:#FFF3E0,stroke:#F57C00,stroke-width:2px
    classDef storage fill:#ECEFF1,stroke:#455A64,stroke-width:2px
    classDef plugins fill:#FCE4EC,stroke:#C2185B,stroke-width:2px

    class JiroCloud,OpenAI,WeatherAPI external
    class PythonCLI,WebClient client
    class JiroApp,RestAPI,GrpcService app
    class ChatService,CmdSystem,WeatherService,PersonaService,ConversationService core
    class EFCore,Repositories,Cache infra
    class SQLite,MySQL storage
    class BaseCommands,CustomPlugins plugins
```

### 🔧 Architecture Components

#### **🚀 Jiro.App - Application Host**

- **Main Host**: Central application with dependency injection and configuration
- **REST API**: Primary interface on port 18090 for client communication
- **gRPC Service**: Optional integration with JiroCloud for distributed commands

#### **💡 Jiro.Core - Business Logic**

- **Chat Service**: Manages conversations and integrates with OpenAI
- **Command System**: Extensible plugin framework for adding new features
- **Weather Service**: Provides location-based weather information
- **Persona Service**: Handles AI personality and behavior customization
- **Conversation Service**: Manages chat context and message history

#### **🏗️ Jiro.Infrastructure - Data Layer**

- **Entity Framework**: ORM for database operations and migrations
- **Repositories**: Clean data access patterns with caching support
- **SQLite**: Default lightweight database, MySQL available via Docker

#### **🔌 Plugin Ecosystem**

- **Base Commands**: Built-in functionality (chat, weather, etc.)
- **Custom Plugins**: Extensible via NuGet packages and the plugin framework
- **Dynamic Loading**: Runtime discovery and registration of new commands

### 🌟 Key Architectural Benefits

- **🏠 Self-Contained**: Runs completely locally with optional cloud features
- **🔌 Extensible**: Plugin system allows easy addition of new commands
- **⚡ Performance**: Memory caching and efficient data access patterns
- **🐳 Deployable**: Docker support with MySQL for production environments
- **🛡️ Flexible**: SQLite for development, MySQL for production scaling

## 📚 Documentation

Explore our comprehensive documentation:

- **[📖 User Guide](dev/docs/)** - Get started and learn how to use Jiro
- **[🔧 API Reference](dev/api/)** - Complete technical documentation
- **[📝 Changelog](dev/docs/changelog/)** - What's new in each version

### Build Documentation Locally

```bash
# Use our handy script
./scripts/docfx-gen.sh        # Linux/macOS
scripts/docfx-gen.ps1         # Windows

# Or manually
cd dev
docfx docfx.json --serve
```

## 🐳 Docker Development

### Quick Start with Docker

The setup script automatically creates Docker configuration:

```bash
# 1. Run the setup script to create .env and config files
./scripts/setup-project.ps1 -Default         # Windows
./scripts/setup-project.sh --default         # Linux/macOS

# 2. Start with Docker Compose (includes MySQL + Jiro Kernel)
docker-compose up -d

# 3. View logs
docker-compose logs -f jiro-kernel

# 4. Stop services
docker-compose down
```

### Environment Configuration

The setup script creates a complete `.env` file with secure defaults:

```bash
# Database Configuration (auto-generated)
MYSQL_ROOT_PASSWORD=secure-generated-password
MYSQL_DATABASE=jiro
MYSQL_USER=jiro
MYSQL_PASSWORD=secure-generated-password
DB_SERVER=mysql

# Application Configuration (auto-generated)
JIRO_ApiKey=secure-generated-api-key
JIRO_JiroApi=https://localhost:5001

# AI Features (configure manually)
OPENAI_API_KEY=your-openai-api-key-here

# Authentication (auto-generated)
JWT_SECRET=secure-generated-32-char-secret
JWT_ISSUER=Jiro
JWT_AUDIENCE=JiroUsers

# Port Configuration
JIRO_HTTP_PORT=8080
JIRO_HTTPS_PORT=8443
JIRO_ADDITIONAL_PORT=18090
MYSQL_PORT=3306

# Data Paths Configuration (optional)
JIRO_DataPaths__Logs=Data/Logs
JIRO_DataPaths__Database=Data/Database/jiro.db
JIRO_DataPaths__Plugins=Data/Plugins
JIRO_DataPaths__Themes=Data/Themes
JIRO_DataPaths__Messages=Data/Messages
```

> **💡 Pro Tip**: Use the `-Default` flag for interactive setup that prompts for your OpenAI API key!

### Data Organization

Jiro uses an organized Data folder structure for all application data:

```sh
Data/
├── Database/     # SQLite database files
├── Logs/         # Application logs (managed by Serilog)
├── Plugins/      # Plugin assemblies and configurations
├── Themes/       # Custom UI themes and styling
└── Messages/     # Markdown files for chat messages
```

**Docker Volumes**: Each Data subfolder has its own persistent volume:

- `jiro_database` → `/home/app/jiro/Data/Database`
- `jiro_logs` → `/home/app/jiro/Data/Logs`
- `jiro_plugins` → `/home/app/jiro/Data/Plugins`
- `jiro_themes` → `/home/app/jiro/Data/Themes`
- `jiro_messages` → `/home/app/jiro/Data/Messages`

**Path Customization**: All paths are configurable via environment variables using the `JIRO_DataPaths__` prefix.

### Production Deployment

- Use `docker-compose.yml` with proper `.env` configuration
- Environment variables override `.env` values for secrets management
- Persistent volumes for all data directories included
- Health checks and proper networking configured
- Organized data structure with separate volumes for each data type

## 🎓 Engineering Thesis Project

This project is developed as part of an engineering thesis focused on building a modern AI assistant platform. The project demonstrates the integration of AI technologies with clean architecture patterns and modern software engineering practices.

## 🛠️ Configuration Reference

> **💡 Note**: The setup script automatically configures most settings. This reference is for manual customization.

### Setup Script Options

| Flag | Description | Best For |
|------|-------------|----------|
| `-Default` / `--default` | Interactive setup with prompts | **Most users** - prompts for API keys |
| `-NonInteractive` / `--non-interactive` | Secure defaults only | **CI/CD** - shows what needs manual config |

### Core API Settings

| Setting | Description | Default | Auto-Generated |
|---------|-------------|---------|----------------|
| `urls` | Hosting URLs | `http://localhost:18090;https://localhost:18091` | ❌ |
| `TokenizerUrl` | Tokenizer API endpoint | `http://localhost:8000` | ❌ |
| `Gpt:AuthToken` | OpenAI API key | *Required for chat features* | ❌ |
| `JWT:Secret` | JWT signing key | *Must be 32+ characters* | ✅ |

### Data Paths Settings

| Setting | Description | Default | Auto-Generated |
|---------|-------------|---------|----------------|
| `DataPaths:Database` | SQLite database file path | `Data/Database/jiro.db` | ❌ |
| `DataPaths:Logs` | Application logs directory | `Data/Logs` | ❌ |
| `DataPaths:Plugins` | Plugin assemblies directory | `Data/Plugins` | ❌ |
| `DataPaths:Themes` | UI themes directory | `Data/Themes` | ❌ |
| `DataPaths:Messages` | Markdown messages directory | `Data/Messages` | ❌ |

### Docker Environment Variables

| Variable | Description | Auto-Generated |
|----------|-------------|----------------|
| `OPENAI_API_KEY` | OpenAI API key for AI features | ❌ (prompted in `-Default` mode) |
| `JWT_SECRET` | JWT signing secret (32+ chars) | ✅ |
| `MYSQL_ROOT_PASSWORD` | MySQL root password | ✅ |
| `MYSQL_PASSWORD` | MySQL user password | ✅ |
| `JIRO_ApiKey` | Jiro API authentication key | ✅ |

### Web Client Settings  

| Setting | Description | Default |
|---------|-------------|---------|
| `PORT` | Development server port | `3000` |
| `JIRO_API` | API proxy target | `https://localhost:18091` |

## ⚡ Common Issues & Solutions

### Setup Issues

**Q: Setup script fails with permission error**

```bash
# Windows
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Linux/macOS  
chmod +x scripts/setup-project.sh
```

**Q: OpenAI API key not working**

- Ensure your API key starts with `sk-`
- Check your OpenAI account has sufficient credits
- Verify the key is set in both `.env` and `appsettings.json`

**Q: Database migration fails**

```bash
# Reset database
dotnet ef database drop -p src/Jiro.Kernel/Jiro.Infrastructure -s src/Jiro.Kernel/Jiro.App --force
dotnet ef database update -p src/Jiro.Kernel/Jiro.Infrastructure -s src/Jiro.Kernel/Jiro.App
```

### Docker Issues

**Q: Docker containers won't start**

```bash
# Check if ports are available
docker-compose down
docker system prune -f
docker-compose up -d
```

**Q: Can't connect to MySQL in Docker**

- Ensure `.env` file has correct MySQL credentials
- Wait for MySQL container to fully initialize (30-60 seconds)

### Development Commands

```bash
# Quick setup for development
./scripts/setup-project.ps1 -Default         # Windows
./scripts/setup-project.sh --default         # Linux/macOS

# Run tests
dotnet test src/Main.sln

# Format code
dotnet format src/Main.sln

# Clean and rebuild
dotnet clean src/Main.sln && dotnet build src/Main.sln
```

## 🎯 Roadmap

- [ ] 🧠 Enhanced AI model support (GPT-4, Claude, etc.)
- [ ] 📱 Mobile applications (iOS/Android)
- [ ] 🔊 Voice interaction capabilities
- [ ] 🌐 Multi-language support
- [ ] 📊 Analytics and usage insights
- [ ] 🤖 Automated plugin marketplace
- [ ] 🔗 Third-party service integrations (Discord, Slack, etc.)

## 💖 Support the Project

If you find this engineering thesis project interesting:

- ⭐ **Star this repository** to show your support
- 🐛 **Report issues** if you find any bugs
- 💬 **Share feedback** on the implementation approach
- 📚 **Check out the documentation** in the `dev/docs/` folder

## 📋 Complete Configuration Reference

This table shows all available configuration variables, where they're stored, and their requirements:

| Variable Name (Section) | Stored In | Required | Description |
|------------------------|-----------|----------|-------------|
| **Main Application** | | | |
| `ApiKey` | appsettings.json | ✅ | Main Jiro instance API key |
| `JIRO_ApiKey` | .env | ✅ | Main Jiro instance API key (env override) |
| **Chat Configuration** | | | |
| `Chat:AuthToken` | appsettings.json | ❌ | OpenAI API key for chat features |
| `Chat:SystemMessage` | appsettings.json | ❌ | AI assistant personality prompt |
| `Chat:TokenLimit` | appsettings.json | ❌ | Maximum tokens per chat response |
| `Chat:Enabled` | appsettings.json | ❌ | Enable/disable chat functionality |
| `JIRO_Chat__AuthToken` | .env | ❌ | OpenAI API key (env override) |
| `JIRO_Chat__SystemMessage` | .env | ❌ | System message (env override) |
| `JIRO_Chat__TokenLimit` | .env | ❌ | Token limit (env override) |
| `JIRO_Chat__Enabled` | .env | ❌ | Chat enabled flag (env override) |
| **Database Configuration** | | | |
| `ConnectionStrings:JiroContext` | appsettings.json | ✅ | SQLite connection string |
| **Data Paths** | | | |
| `DataPaths:Logs` | appsettings.json | ❌ | Application logs directory |
| `DataPaths:Messages` | appsettings.json | ❌ | Chat messages storage directory |
| `DataPaths:Plugins` | appsettings.json | ❌ | Plugin assemblies directory |
| `DataPaths:Themes` | appsettings.json | ❌ | UI themes directory |
| `JIRO_DataPaths__Logs` | .env | ❌ | Logs path (env override) |
| `JIRO_DataPaths__Messages` | .env | ❌ | Messages path (env override) |
| `JIRO_DataPaths__Plugins` | .env | ❌ | Plugins path (env override) |
| `JIRO_DataPaths__Themes` | .env | ❌ | Themes path (env override) |
| **JiroCloud Configuration** | | | |
| `JiroCloud:ApiKey` | appsettings.json | ✅ | JiroCloud API key for cloud services |
| `JIRO_JiroCloud__ApiKey` | .env | ✅ | JiroCloud API key (env override) |
| **JiroCloud gRPC** | | | |
| `JiroCloud:Grpc:ServerUrl` | appsettings.json | ✅ | JiroCloud gRPC server URL |
| `JiroCloud:Grpc:MaxRetries` | appsettings.json | ❌ | Maximum gRPC retry attempts |
| `JiroCloud:Grpc:TimeoutMs` | appsettings.json | ❌ | gRPC timeout in milliseconds |
| `JIRO_JiroCloud__Grpc__ServerUrl` | .env | ✅ | gRPC server URL (env override) |
| `JIRO_JiroCloud__Grpc__MaxRetries` | .env | ❌ | Max retries (env override) |
| `JIRO_JiroCloud__Grpc__TimeoutMs` | .env | ❌ | Timeout (env override) |
| **JiroCloud WebSocket** | | | |
| `JiroCloud:WebSocket:HubUrl` | appsettings.json | ✅ | SignalR hub URL |
| `JiroCloud:WebSocket:HandshakeTimeoutMs` | appsettings.json | ❌ | WebSocket handshake timeout |
| `JiroCloud:WebSocket:KeepAliveIntervalMs` | appsettings.json | ❌ | Keep-alive interval |
| `JiroCloud:WebSocket:ReconnectionAttempts` | appsettings.json | ❌ | Max reconnection attempts |
| `JiroCloud:WebSocket:ReconnectionDelayMs` | appsettings.json | ❌ | Delay between reconnections |
| `JiroCloud:WebSocket:ServerTimeoutMs` | appsettings.json | ❌ | Server timeout |
| `JIRO_JiroCloud__WebSocket__HubUrl` | .env | ✅ | Hub URL (env override) |
| `JIRO_JiroCloud__WebSocket__HandshakeTimeoutMs` | .env | ❌ | Handshake timeout (env override) |
| `JIRO_JiroCloud__WebSocket__KeepAliveIntervalMs` | .env | ❌ | Keep-alive interval (env override) |
| `JIRO_JiroCloud__WebSocket__ReconnectionAttempts` | .env | ❌ | Reconnection attempts (env override) |
| `JIRO_JiroCloud__WebSocket__ReconnectionDelayMs` | .env | ❌ | Reconnection delay (env override) |
| `JIRO_JiroCloud__WebSocket__ServerTimeoutMs` | .env | ❌ | Server timeout (env override) |

### Configuration Notes

- **✅ Required**: Must be configured for the application to function properly
- **❌ Optional**: Has sensible defaults or is optional functionality
- **Auto-generated**: The setup script automatically generates secure values for API keys
- **JIRO_ Prefix**: All environment variables use the `JIRO_` prefix with `__` for nested sections
- **Environment Override**: Environment variables take precedence over `appsettings.json` values

### Environment Variable Hierarchy

1. **Environment Variables** (highest priority) - `JIRO_` prefixed with `__` for sections
2. **appsettings.json** (fallback) - Standard JSON configuration

### Configuration Examples

```bash
# Environment variables override appsettings.json
JIRO_Chat__AuthToken=sk-your-openai-key
JIRO_JiroCloud__ApiKey=your-jirocloud-api-key
JIRO_JiroCloud__Grpc__ServerUrl=https://api.jirocloud.com
JIRO_DataPaths__Logs=/custom/logs/path
```

### Setup Script Benefits

- **Secure Generation**: Auto-generates cryptographically secure API keys
- **Consistent Structure**: Ensures both `.env` and `appsettings.json` are synchronized
- **Environment Ready**: Creates proper JIRO_ prefixed environment variables

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- 🤖 **OpenAI** for the powerful GPT models
- 🚀 **Microsoft** for the amazing .NET ecosystem
- ❤️ **The open-source community** for inspiration and support

---

<p align="center">
    <strong>Ready to meet your new AI assistant? <a href="#-quick-start">Get Started</a> | <a href="dev/docs/">Read the Docs</a> | <a href="https://github.com/HueByte/Jiro/issues">Get Help</a></strong>
</p>

<p align="center">
    Made with ❤️ by <a href="https://github.com/HueByte">HueByte</a>
</p>
