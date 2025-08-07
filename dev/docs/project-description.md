# 🤖 Jiro - Advanced AI Virtual Assistant

![Jiro Banner](~/assets/JiroBanner.png)

## 📋 Table of Contents

- [Project Overview](#-project-overview)
- [Architecture](#️-architecture)
- [Core Technologies](#-core-technologies)
- [Core Services](#️-core-services)
- [Command System](#-command-system)
- [Communication Protocols](#-communication-protocols)
- [Infrastructure & Database](#️-infrastructure--database)
- [Deployment & Docker](#-deployment--docker)
- [AI & Machine Learning](#-ai--machine-learning)
- [Testing Framework](#-testing-framework)
- [Development Workflow](#-development-workflow)
- [Project Structure](#-project-structure)

## 🌟 Project Overview

**Jiro v1.0.0-beta "Kakushin"** is a production-ready, enterprise-grade AI virtual assistant platform that combines the power of Large Language Models (LLMs) with a robust custom command system and extensible plugin architecture. Built on modern .NET 9 technologies following Clean Architecture principles, Jiro provides a comprehensive solution for AI-powered assistance, featuring real-time bidirectional communication, intelligent conversation management, and cloud-native deployment capabilities.

### Key Features (v1.0.0-beta)

- **AI-Powered Conversations**: Integration with state-of-the-art LLMs for intelligent dialogue
- **Plugin System**: Extensible command architecture for custom functionality
- **Real-time Communication**: WebSocket + gRPC hybrid architecture for instant bidirectional streaming
- **Real-time Log Streaming**: Continuous log monitoring with `StreamLogsAsync` and batch delivery
- **Enhanced Session Management**: Client-side session ID generation with advanced caching
- **Multi-Client Support**: Web, CLI, and API interfaces with SignalR hub integration
- **Service Architecture**: Separated SessionManager, MessageCacheService, and LogsProviderService
- **Intelligent Memory Management**: Conversation history optimization and persona management
- **Weather Integration**: Real-time weather data with geolocation services
- **Token Management**: Advanced token counting and optimization for cost efficiency
- **Docker Profiles**: Multi-profile deployment (default, docs, full) with comprehensive environment configuration
- **90+ Configuration Options**: Flexible deployment with JIRO_ prefixed environment variables

## 🏗️ Architecture

Jiro follows a **clean architecture** pattern with clear separation of concerns:

```cs
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Presentation  │    │    Application  │    │   Infrastructure│
│                 │    │                 │    │                 │
│ • gRPC Services │◄──►│ • Services      │◄──►│ • Repositories  │
│ • Controllers   │    │ • Commands      │    │ • HTTP Clients  │
│ • Client Apps   │    │ • Handlers      │    │ • Database      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
          │                        │                        │
          └──────────┬───────────────────────────────────────┘
                     │
          ┌─────────────────┐
          │      Domain     │
          │                 │
          │ • Models        │
          │ • Interfaces    │
          │ • Constants     │
          └─────────────────┘
```

## 🔧 Core Technologies

### **Backend Framework**

- **.NET 9.0**: Latest .NET framework with performance improvements
- **ASP.NET Core**: Web API and hosting infrastructure
- **Entity Framework Core**: ORM for database operations
- **gRPC**: High-performance RPC framework for real-time communication

### **AI & Machine Learning**

- **LLM Integration**: Support for advanced language models via OpenAI API
- **Custom Token Management**: Tiktoken integration for token counting
- **Conversation Optimization**: Intelligent history management

### **Communication & Protocols**

- **gRPC Streaming**: Bidirectional streaming for real-time communication
- **Protocol Buffers**: Efficient serialization format
- **HTTP/2**: Modern HTTP protocol support

### **Data & Caching**

- **MySQL 8.0**: Primary relational database
- **In-Memory Caching**: High-performance memory caching
- **Entity Framework Migrations**: Database schema management

### **Containerization & Deployment**

- **Docker**: Multi-stage containerization
- **Docker Compose**: Orchestration for development and production
- **Linux Containers**: Optimized runtime environment

## 🛠️ Core Services

### **Conversation Management**

#### **ConversationCoreService**

- **Purpose**: Core conversation logic with OpenAI integration
- **Features**:
  - Direct chat completion with LLM models
  - Token usage tracking and optimization
  - Semaphore-based concurrency control
  - Temperature and parameter management
- **Key Methods**:
  - `ChatAsync()`: Full conversation with history
  - `ExchangeMessageAsync()`: Single message exchange

#### **PersonalizedConversationService**

- **Purpose**: Session-based personalized conversations
- **Features**:
  - Session management and persistence
  - Message history optimization
  - Persona integration
  - Cost tracking and optimization
- **Advanced Capabilities**:

#### **Advanced Capabilities**

- Automatic history pruning when token limits are exceeded
- Dynamic persona updates based on conversation summaries
- Real-time pricing calculations
  - Dynamic persona updates based on conversation summaries
  - Real-time pricing calculations

### **Message & Session Management (v1.0.0-beta)**

#### **SessionManager**

- **Purpose**: Dedicated chat session lifecycle management
- **Features**:
  - Advanced session caching with 5-day default expiration
  - Session state management with persistence and recovery
  - Multi-session support with user-based isolation
  - Session metadata tracking and analytics

#### **MessageCacheService**

- **Purpose**: Specialized message operations and optimization
- **Features**:
  - Message history optimization with performance-focused caching
  - Message exchange handling with thread-safe operations
  - Memory-efficient message storage with automatic cleanup
  - Message retrieval optimization for large conversation histories

#### **CompositeMessageManager**

- **Purpose**: Unified message management orchestration
- **Features**:
  - Coordinates between SessionManager and MessageCacheService
  - Provides high-level message operations with service abstraction
  - Handles complex message workflows and cross-service coordination

### **AI Persona System**

#### **PersonaService**

- **Purpose**: Dynamic AI personality management
- **Features**:
  - Core persona message management
  - Conversation summary integration
  - Cache-based performance optimization
  - Thread-safe persona updates
- **Intelligence Features**:
  - Adaptive persona based on conversation history
  - Automatic summary generation for long conversations

### **Weather & Geolocation Services**

#### **WeatherService**

- **Purpose**: Real-time weather data integration
- **Data Sources**:
  - **Open-Meteo API**: Weather forecasting data
  - **OpenStreetMap Nominatim**: Geolocation services
- **Features**:
  - Location-based weather queries
  - Multi-day forecasting (up to 7 days)
  - Structured weather data for graphing
  - Temperature, rainfall, and wind speed tracking

#### **GeolocationService**

- **Purpose**: City-to-coordinates conversion
- **Features**:
  - City name to latitude/longitude resolution
  - Support for international locations
  - Error handling for invalid locations

### **Command System Services**

#### **CommandHandlerService**

- **Purpose**: Plugin command execution and management
- **Features**:
  - Command parsing with regex pattern matching
  - Dynamic command discovery and execution
  - Exception handling and error reporting
  - Performance monitoring with execution timing
- **Command Types**:
  - **Text Commands**: Simple text responses
  - **Graph Commands**: Data visualization responses

#### **HelpService**

- **Purpose**: Dynamic help system generation
- **Features**:
  - Automatic help message generation from registered commands
  - Command description and usage examples
  - Runtime command discovery

### **Infrastructure Services**

#### **SemaphoreManager**

- **Purpose**: Concurrency control for chat instances
- **Features**:
  - Per-instance semaphore management
  - Thread-safe operations
  - Resource contention prevention

#### **HistoryOptimizerService**

- **Purpose**: Intelligent conversation history management
- **Features**:
  - Token-based optimization decisions
  - Message summarization
  - History pruning strategies

## ⚡ Command System

### **Plugin Architecture**

Jiro uses a sophisticated plugin system based on the **Jiro.Commands** framework:

#### **Command Attributes**

```csharp
[CommandModule("Weather")]
public class WeatherCommand : ICommandBase
{
    [Command("weather", CommandType.Graph, "weather \"Location\" [daysRange]", 
             "Shows weather forecast for the specified location")]
    public async Task<ICommandResult> Weather(string location, int daysRange)
    {
        // Implementation
    }
}
```

#### **Built-in Commands**

##### **Chat Commands**

- `chat <message>`: AI conversation
- `getSessions`: Retrieve user chat sessions
- `getSessionHistory <sessionId>`: Get specific session history
- `reset`: Clear current session

##### **Weather Commands**

- `weather "Location" [days]`: Weather forecast with graphical data
  - Returns temperature, rainfall, and wind speed
  - Supports 1-7 day forecasting
  - Includes current weather conditions

### **Command Processing Pipeline**

1. **Input Parsing**: Regex-based token extraction
2. **Command Resolution**: Dynamic command discovery
3. **Parameter Binding**: Automatic parameter mapping
4. **Execution**: Async command execution with error handling
5. **Result Formatting**: Type-specific result formatting

## 📡 Communication Protocols

### **gRPC Service Definition**

#### **JiroHubProto Service**

```proto
service JiroHubProto {
    rpc GetUserSessions (Empty) returns (SessionsReply);
    rpc InstanceCommand (stream ClientMessage) returns (stream ServerMessage);
    rpc Hello (HelloRequest) returns (HelloReply);
}
```

#### **Message Types**

- **ClientMessage**: Command requests with parameters and session info
- **ServerMessage**: Command responses with execution results
- **Result Types**:
  - `TextResult`: Simple text responses
  - `GraphResult`: Data visualization with metadata

#### **Streaming Architecture**

- **Bidirectional Streaming**: Real-time command execution
- **Session Management**: Per-session command queuing
- **Error Handling**: Graceful error propagation

### **Communication Clients**

#### **Python CLI Client** (`Jiro.Communication`)

- **Features**:
  - Interactive command-line interface
  - Real-time streaming responses
  - Graph visualization for weather data
  - Color-coded output formatting

#### **Token API** (`Jiro.TokenApi`)

- **FastAPI-based service** for token management
- **Endpoints**:
  - `/reduce`: Message history optimization
  - `/tokenize`: Token counting for cost estimation
- **Features**:
  - Tiktoken integration for accurate LLM token counting
  - Automatic message pruning when limits exceeded

## 🗄️ Infrastructure & Database

### **Database Schema**

Jiro utilizes **ASP.NET Core Identity** for user management combined with custom business entities for conversation management.

#### **Core Database Tables**

##### **Identity System**

- **AspNetUsers**: User accounts with extended properties (`AccountCreatedDate`)
- **AspNetRoles**: System roles and permissions
- **AspNetUserRoles**: Many-to-many user-role relationships

##### **Conversation System**  

- **ChatSessions**: Conversation sessions with metadata
- **Messages**: Individual messages with type classification and user attribution

#### **Key Relationships**

- `Users 1:N RefreshTokens` - Token-based authentication
- `ChatSessions 1:N Messages` - Session-based conversations
- `Users M:N Roles` - Role-based authorization

For detailed database schema documentation, see [Database Schema](database-schema.md).

### **Entity Framework Configuration**

- **Code-First Migrations**: Version-controlled schema changes
- **Connection String Management**: Environment-based configuration
- **Repository Pattern**: Clean data access abstraction

### **Caching Strategy**

- **In-Memory Cache**: Fast access to frequently used data
- **TTL-based Expiration**: Automatic cache invalidation
- **Cache Keys**: Structured caching with prefixed keys

## 🐳 Deployment & Docker

### **Multi-Stage Docker Build**

```dockerfile
# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ... build process

# Runtime Stage  
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
# ... optimized runtime environment
```

### **Docker Compose Architecture**

```yaml
services:
  jiro-kernel:     # Main application
  mysql:           # Database server
  # Future: jiro-tokenapi, jiro-communication
```

### **Production Optimizations**

- **Self-contained deployment**: No runtime dependencies
- **Trimmed assemblies**: Reduced image size
- **Non-root user**: Security best practices
- **Health checks**: Container health monitoring
- **Volume mounts**: Persistent data and logs

## 🧠 AI & Machine Learning

### **OpenAI Integration**

- **Models**: Multiple LLM options including latest generation models
- **Temperature Control**: Configurable response creativity (default: 0.6)
- **Token Management**: Advanced cost optimization
- **Context Handling**: Intelligent conversation context management

### **Token Optimization**

- **Real-time Token Counting**: Accurate cost prediction
- **History Pruning**: Automatic conversation trimming
- **Cache Optimization**: Reduced API calls through caching
- **Cost Tracking**: Per-message pricing calculations

### **Conversation Intelligence**

- **Persona Management**: Dynamic AI personality adaptation
- **Summary Generation**: Automatic conversation summarization
- **Context Preservation**: Intelligent history optimization
- **Session Continuity**: Seamless multi-session conversations

## 🧪 Testing Framework

### **Comprehensive Test Suite**

- **Unit Tests**: Service-level testing with mocking
- **Integration Tests**: End-to-end scenario testing
- **Service Tests**: Individual service validation

### **Tested Components**

- `ConversationCoreServiceTests`
- `PersonalizedConversationServiceTests`
- `WeatherServiceTests`
- `CommandHandlerServiceTests`
- `MessageManagerTests`
- `PersonaServiceTests`
- `GeolocationServiceTests`

### **Testing Technologies**

- **xUnit**: Primary testing framework
- **Moq**: Mocking framework for dependencies
- **In-Memory Database**: Isolated test data

## 🔄 Development Workflow

### **Build & Run Process**

1. **Configuration Setup**: `appsettings.json` with OpenAI keys
2. **Database Migration**: Automatic schema updates
3. **Service Registration**: Dependency injection configuration
4. **gRPC Service Startup**: Real-time communication initialization

### **Development Tools**

- **Hot Reload**: Real-time code updates
- **Logging**: Structured logging with Serilog
- **Health Checks**: Service health monitoring
- **API Documentation**: Auto-generated from gRPC proto files

### **Plugin Development**

1. **Reference Jiro.Commands NuGet package**
2. **Implement ICommandBase interface**
3. **Add command attributes**
4. **Register in module configuration**

## 📁 Project Structure

```text
Jiro/
├── src/                           # Source code
│   ├── Jiro.Kernel/              # Main application kernel
│   │   ├── Jiro.App/            # Web API & gRPC host (v0.1.2)
│   │   ├── Jiro.Core/           # Business logic & services
│   │   └── Jiro.Infrastructure/ # Data access & external services
│   ├── Jiro.Communication/       # Python CLI client
│   ├── Jiro.TokenApi/           # Token management FastAPI service
│   ├── Jiro.Tests/              # Comprehensive test suite
│   └── Main.sln                 # Solution file
├── .github/                      # GitHub automation & workflows
│   ├── workflows/               # CI/CD pipeline definitions
│   │   ├── jiro-kernel-ci.yml       # 🔧 .NET CI (build, test, quality gate)
│   │   ├── create-release.yml       # 🚀 Version-based releases & artifacts
│   │   ├── docker-build.yml         # 🐳 Container build & security scanning
│   │   ├── markdown-lint.yml        # 📝 Documentation quality assurance
│   │   ├── jiro-kernel-security.yml # 🔒 Security vulnerability scanning
│   │   ├── deploy-docs.yml          # 📚 Documentation deployment
│   │   └── jiro-kernel-performance.yml # 📊 Performance monitoring
│   └── WORKFLOWS.md             # Workflow documentation
├── docs/                        # Documentation content
│   ├── project-description.md   # This comprehensive project overview
│   ├── user-guide.md           # End-user documentation  
│   ├── workflow-pipelines.md   # ☁️ CI/CD automation documentation
│   ├── database-schema.md      # 🗄️ Complete database schema documentation
│   ├── api-index.md            # API documentation index
│   └── README.md               # Documentation setup guide
├── assets/                      # Documentation assets (logos, images)
├── scripts/                     # Build and deployment scripts
├── _site/                       # DocFX build output (auto-generated)
├── api/                         # Generated API documentation (auto-generated)
├── docfx.json                  # DocFX documentation configuration
├── toc.yml                     # Main navigation structure
├── index.md                    # Documentation homepage
├── docker-compose.yml          # Container orchestration
├── filterConfig.yml            # API documentation filter
└── README.md                   # Project setup and usage guide
```

### **🔧 Core Application Architecture**

#### **Jiro.Core Services** (Business Logic Layer)

- `Services/Conversation/`: AI conversation management
  - `ConversationCoreService`: Direct OpenAI integration
  - `PersonalizedConversationService`: Session-based conversations
  - `HistoryOptimizerService`: Token optimization
- `Services/Weather/`: Weather and geolocation services
- `Services/MessageCache/`: Session and message management  
- `Services/Persona/`: AI personality system
- `Services/CommandSystem/`: Plugin command framework
- `Services/Semaphore/`: Concurrency control

#### **Jiro.App** (Presentation Layer)

- `Controllers/`: Web API endpoints
- `Services/`: gRPC service implementations
- `Configurator/`: Application startup and dependency injection
- `Proto/`: Protocol buffer definitions

#### **Jiro.Infrastructure** (Data Access Layer)

- `Repositories/`: Entity Framework repositories
- `Migrations/`: Database schema management
- `JiroContext.cs`: Database context configuration

#### **Command Modules** (Plugin System)

- `Commands/Chat/`: Conversation commands (`chat`, `getSessions`, `reset`)
- `Commands/Weather/`: Weather data commands (`weather`)
- `Commands/BaseCommands/`: Command framework infrastructure
- `Commands/Net/`: Network utility commands

### **🚀 CI/CD Pipeline Architecture**

#### **Automated Workflows Organization**

```text
Pull Request → Multiple Validation Workflows (Parallel)
    ├── .NET CI: Build, Test, Format, Quality Gate
    ├── Docker Build: Container build & security scan  
    ├── Markdown Lint: Documentation quality
    ├── Security Scan: CodeQL, Snyk, .NET audit
    └── Release Validation: Additional pre-merge checks

Merge to Main → Production Workflows (Sequential)
    ├── Manual Version Control: Developer-controlled release timing
    ├── Release Artifacts: Multi-platform binaries (Linux, Windows, macOS)
    ├── Docker Push: Container registry deployment
    ├── Documentation Deploy: GitHub Pages update
    └── Security Monitoring: Ongoing vulnerability tracking
```

#### **Deployment Artifacts**

- **🐳 Container Images**: `ghcr.io/huebyte/jiro-kernel:latest`
- **📦 GitHub Releases**: Manual version-controlled releases with multi-platform binaries
  - Linux x64: `jiro-kernel-vX.X.X-linux-x64.tar.gz`
  - Windows x64: `jiro-kernel-vX.X.X-win-x64.zip`
  - macOS x64: `jiro-kernel-vX.X.X-osx-x64.tar.gz`
- **📚 Documentation**: Live API docs on GitHub Pages
- **🔒 Security Reports**: Continuous vulnerability monitoring

### **🛡️ Quality Assurance & Security**

#### **Automated Quality Gates**

- **Pre-merge**: Build verification, test execution, code formatting
- **Security**: CodeQL analysis, dependency scanning, container security
- **Documentation**: Markdown linting, API doc generation
- **Performance**: NuGet caching, parallel execution, smart triggers

#### **Development Best Practices**

- **Clean Architecture**: Clear separation of concerns across layers
- **Dependency Injection**: Configured in `Jiro.App/Configurator/`
- **Entity Framework**: Code-first migrations with repository pattern  
- **Testing**: Comprehensive unit and integration test coverage
- **Documentation**: Auto-generated from XML comments and markdown

This architecture ensures scalable development with automated quality assurance, security monitoring, and deployment automation through the comprehensive ☁️ CI/CD pipeline.

- `Options/`: Configuration models

---

## 🚀 Getting Started

To get started with Jiro development:

1. **Clone the repository**
2. **Configure OpenAI API key** in `appsettings.json`
3. **Run database migrations**
4. **Start the application** with `dotnet run`
5. **Connect clients** via gRPC or Python CLI

For detailed setup instructions, refer to the main project README.

---

**Jiro** represents a modern approach to AI assistant development as part of an engineering thesis project. It combines cutting-edge technologies with robust engineering practices to deliver a scalable, intelligent, and extensible platform for AI-powered assistance, demonstrating the practical application of clean architecture patterns and modern software development methodologies.
