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
- OpenAI API Key (optional, for chat features)

### 🏃‍♂️ Get Running in 5 Minutes

1. **Clone the repository**

   ```bash
   git clone https://github.com/HueByte/Jiro.git
   cd Jiro
   ```

2. **Set up configuration**

   ```bash
   # Navigate to the app directory
   cd src/Jiro.Kernel/Jiro.App
   
   # Copy example configs
   cp appsettings.example.json appsettings.json
   cd clientapp
   cp envExamples/.env.example .env
   cp envExamples/.env.development.example .env.development
   ```

3. **Configure your OpenAI key** (optional)

   ```json
   // In appsettings.json
   {
     "Gpt": {
       "AuthToken": "your-openai-api-key-here"
     }
   }
   ```

4. **Run Jiro**

   ```bash
   cd ../  # Back to Jiro.App directory
   dotnet tool restore
   dotnet run
   ```

5. **Start the tokenizer** (in a new terminal)

   ```bash
   cd src/Jiro.TokenApi
   pip install -r requirements.txt
   python main.py
   ```

6. **Open your browser** and navigate to `https://localhost:5001` 🎉

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

📚 **Learn More**: Check out our [Plugin Development Guide](https://github.com/HueByte/Jiro.Libs) and [NuGet Package](https://www.nuget.org/packages/Jiro.Commands/)

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

## 🤝 Contributing

We love contributions! Here's how you can help make Jiro even better:

1. 🍴 **Fork the repository**
2. 🌿 **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. ✨ **Make your changes**
4. ✅ **Add tests** (if applicable)
5. 📝 **Commit your changes** (`git commit -m 'Add amazing feature'`)
6. 📤 **Push to the branch** (`git push origin feature/amazing-feature`)
7. 🎉 **Open a Pull Request**

### Areas We'd Love Help With

- 🔌 New plugin ideas and implementations
- 🌍 Internationalization and localization
- 📱 Mobile app development
- 🎨 UI/UX improvements
- 📚 Documentation and tutorials
- 🐛 Bug fixes and performance improvements

## 🛠️ Configuration Reference

### Core API Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `urls` | Hosting URLs | `http://localhost:18090;https://localhost:18091` |
| `TokenizerUrl` | Tokenizer API endpoint | `http://localhost:8000` |
| `Gpt:AuthToken` | OpenAI API key | *Required for chat features* |
| `JWT:Secret` | JWT signing key | *Change in production!* |

### Web Client Settings  

| Setting | Description | Default |
|---------|-------------|---------|
| `PORT` | Development server port | `3000` |
| `JIRO_API` | API proxy target | `https://localhost:18091` |

## 🎯 Roadmap

- [ ] 🧠 Enhanced AI model support (GPT-4, Claude, etc.)
- [ ] 📱 Mobile applications (iOS/Android)
- [ ] 🔊 Voice interaction capabilities
- [ ] 🌐 Multi-language support
- [ ] 📊 Analytics and usage insights
- [ ] 🤖 Automated plugin marketplace
- [ ] 🔗 Third-party service integrations (Discord, Slack, etc.)

## 💖 Support the Project

If Jiro has helped you or you think it's awesome, consider:

- ⭐ **Starring this repository**
- 🐛 **Reporting bugs** and suggesting features
- 💬 **Sharing it** with friends and colleagues
- 🔌 **Creating plugins** for the community

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- 🤖 **OpenAI** for the powerful GPT models
- 🚀 **Microsoft** for the amazing .NET ecosystem
- 🌟 **All contributors** who help make Jiro better
- ❤️ **The open-source community** for inspiration and support

---

<p align="center">
    <strong>Ready to meet your new AI assistant? <a href="#-quick-start">Get Started</a> | <a href="dev/docs/">Read the Docs</a> | <a href="https://github.com/HueByte/Jiro/issues">Get Help</a></strong>
</p>

<p align="center">
    Made with ❤️ by <a href="https://github.com/HueByte">HueByte</a>
</p>
