# Jiro Project Structure

This document shows the complete project structure for the Jiro AI Assistant project.
Generated on 2025-07-11 00:48:50 using eza with git-aware filtering.

## Key Components

- **src/Jiro.Kernel/**: Main application kernel containing core services
- **src/Jiro.Communication/**: Python communication layer and graph generation
- **assets/**: Project assets including images and documentation diagrams
- **docs/**: Project documentation and user guides
- **scripts/**: Build and deployment scripts

## Project Tree

```jsx
.
├── assets
│   ├── Jiro0-40px.png
│   ├── Jiro0.png
│   ├── Jiro1.png
│   ├── JiroBanner.png
│   ├── JiroDevFlow.excalidraw
│   └── JiroDevFlow.png
├── LICENSE
├── PROJECT_STRUCTURE.md
├── README.md
├── scripts
│   ├── create-release.ps1
│   ├── create-release.sh
│   ├── database-schema.sql
│   ├── local-ci-test.ps1
│   └── local-ci-test.sh
└── src
    ├── api
    │   └── index.md
    ├── docfx.json
    ├── docker-compose.yml
    ├── docs
    │   ├── api-index.md
    │   ├── changelog.md
    │   ├── database-erd.md
    │   ├── database-schema.md
    │   ├── project-description.md
    │   ├── README.md
    │   ├── toc.yml
    │   ├── user-guide.md
    │   └── workflow-pipelines.md
    ├── filterConfig.yml
    ├── index.md
    ├── Jiro.Communication
    │   ├── config.json
    │   ├── graphs.py
    │   ├── jiro.py
    │   ├── lib.py
    │   ├── main.py
    │   ├── models
    │   │   └── jiro_models.py
    │   ├── requirements.txt
    │   └── sharedStorage.py
    ├── Jiro.Kernel
    │   ├── Dockerfile
    │   ├── Jiro.App
    │   │   ├── appsettings.example.json
    │   │   ├── appsettings.json
    │   │   ├── Configurator
    │   │   │   ├── AppConfigurator.cs
    │   │   │   ├── Configurator.cs
    │   │   │   ├── EnvironmentConfigurator.cs
    │   │   │   └── EventsConfigurator.cs
    │   │   ├── Jiro.App.csproj
    │   │   ├── JiroClientService.cs
    │   │   ├── Messages
    │   │   │   └── JIRO_Persona.md
    │   │   ├── Program.cs
    │   │   └── Proto
    │   │       └── jiroHub.proto
    │   ├── Jiro.Core
    │   │   ├── Abstraction
    │   │   │   ├── BaseRepository.cs
    │   │   │   ├── DbModel.cs
    │   │   │   ├── IdentityBaseRepository.cs
    │   │   │   ├── IdentityDbModel.cs
    │   │   │   ├── IIdentityRepository.cs
    │   │   │   └── IRepository.cs
    │   │   ├── Attributes
    │   │   │   └── AnomifyAttribute.cs
    │   │   ├── Commands
    │   │   │   ├── BaseCommands
    │   │   │   │   └── BaseCommand.cs
    │   │   │   ├── Chat
    │   │   │   │   └── ChatCommand.cs
    │   │   │   ├── ComplexCommandResults
    │   │   │   │   ├── SessionResult.cs
    │   │   │   │   └── TrimmedMessageResult.cs
    │   │   │   ├── Net
    │   │   │   │   └── NetCommands.cs
    │   │   │   └── Weather
    │   │   │       └── WeatherCommand.cs
    │   │   ├── Constants
    │   │   │   ├── ApiEndpoints.cs
    │   │   │   ├── BaseConstants.cs
    │   │   │   ├── CookieNames.cs
    │   │   │   ├── HttpClientNames.cs
    │   │   │   ├── Policies.cs
    │   │   │   └── Roles.cs
    │   │   ├── DTO
    │   │   │   ├── AssignRoleDTO.cs
    │   │   │   ├── ChangeEmailDTO.cs
    │   │   │   ├── ChangePasswordDTO.cs
    │   │   │   ├── InstanceConfigDTO.cs
    │   │   │   ├── JiroPromptDto.cs
    │   │   │   ├── LoginUsernameDTO.cs
    │   │   │   ├── RegisterDTO.cs
    │   │   │   ├── UserIdDTO.cs
    │   │   │   ├── UserInfoDTO.cs
    │   │   │   ├── VerifiedUserDTO.cs
    │   │   │   └── WhitelistedUserDTO.cs
    │   │   ├── Exceptions.cs
    │   │   ├── Globals.cs
    │   │   ├── IRepositories
    │   │   │   ├── IChatSessionRepository.cs
    │   │   │   └── IMessageRepository.cs
    │   │   ├── Jiro.Core.csproj
    │   │   ├── Models
    │   │   │   ├── AppRole.cs
    │   │   │   ├── AppUser.cs
    │   │   │   ├── AppUserRole.cs
    │   │   │   ├── ChatSession.cs
    │   │   │   ├── Message.cs
    │   │   │   ├── MessageType.cs
    │   │   │   └── RefreshToken.cs
    │   │   ├── Options
    │   │   │   ├── BotOptions.cs
    │   │   │   ├── ChatOptions.cs
    │   │   │   ├── GptOptions.cs
    │   │   │   ├── IOption.cs
    │   │   │   ├── JWTOptions.cs
    │   │   │   └── LogOptions.cs
    │   │   ├── Services
    │   │   │   ├── Admin
    │   │   │   │   └── AdminService.cs
    │   │   │   ├── Chat
    │   │   │   ├── Commandcontext
    │   │   │   │   ├── CommandContext.cs
    │   │   │   │   └── ICommandContext.cs
    │   │   │   ├── CommandSystem
    │   │   │   │   ├── CommandHandlerService.cs
    │   │   │   │   ├── HelpService.cs
    │   │   │   │   ├── ICommandHandlerService.cs
    │   │   │   │   └── IHelpService.cs
    │   │   │   ├── Conversation
    │   │   │   │   ├── ConversationCoreService.cs
    │   │   │   │   ├── HistoryOptimizerService.cs
    │   │   │   │   ├── IConversationCoreService.cs
    │   │   │   │   ├── IHistoryOptimizerService.cs
    │   │   │   │   ├── IPersonalizedConversationService.cs
    │   │   │   │   ├── Models
    │   │   │   │   │   ├── ChatMessageWithMetadata.cs
    │   │   │   │   │   ├── OptimizerResult.cs
    │   │   │   │   │   └── Session.cs
    │   │   │   │   └── PersonalizedConversationService.cs
    │   │   │   ├── Geolocation
    │   │   │   │   ├── GeolocationService.cs
    │   │   │   │   └── IGeolocationService.cs
    │   │   │   ├── MessageCache
    │   │   │   │   ├── IMessageManager.cs
    │   │   │   │   └── MessageManager.cs
    │   │   │   ├── Persona
    │   │   │   │   ├── IPersonaService.cs
    │   │   │   │   └── PersonaService.cs
    │   │   │   ├── Semaphore
    │   │   │   │   ├── ISemaphoreManager.cs
    │   │   │   │   └── SemaphoreManager.cs
    │   │   │   ├── Supervisor
    │   │   │   │   └── SupervisorService.cs
    │   │   │   └── Weather
    │   │   │       ├── IWeatherService.cs
    │   │   │       ├── Models
    │   │   │       │   ├── GeoLocationResponse.cs
    │   │   │       │   ├── WeatherGraphData.cs
    │   │   │       │   └── WeatherResponse.cs
    │   │   │       └── WeatherService.cs
    │   │   └── Utils
    │   │       ├── AppUtils.cs
    │   │       └── Tokenizer.cs
    │   ├── Jiro.Infrastructure
    │   │   ├── Extensions.cs
    │   │   ├── Jiro.Infrastructure.csproj
    │   │   ├── JiroContext.cs
    │   │   ├── Migrations
    │   │   │   ├── 20230708005421_init.cs
    │   │   │   ├── 20230708005421_init.Designer.cs
    │   │   │   ├── 20240817164704_ChatSessions.cs
    │   │   │   ├── 20240817164704_ChatSessions.Designer.cs
    │   │   │   ├── 20240817165129_ChatSessions_Clear.cs
    │   │   │   ├── 20240817165129_ChatSessions_Clear.Designer.cs
    │   │   │   ├── 20250613174948_session_adjustments.cs
    │   │   │   ├── 20250613174948_session_adjustments.Designer.cs
    │   │   │   ├── 20250613175025_refresh_token.cs
    │   │   │   ├── 20250613175025_refresh_token.Designer.cs
    │   │   │   └── JiroContextModelSnapshot.cs
    │   │   └── Repositories
    │   │       ├── ChatSessionRepository.cs
    │   │       └── MessageRepository.cs
    │   └── Jiro.Kernel.sln
    ├── Jiro.Tests
    │   ├── Jiro.Tests.csproj
    │   ├── ServiceTests
    │   │   ├── AdminServiceTests.cs
    │   │   ├── CommandHandlerServiceTests.cs
    │   │   ├── ConversationCoreServiceTests.cs
    │   │   ├── GeolocationServiceTests.cs
    │   │   ├── HelpServiceTests.cs
    │   │   ├── HistoryOptimizerServiceTests.cs
    │   │   ├── MessageManagerTests.cs
    │   │   ├── PersonalizedConversationServiceTests.cs
    │   │   ├── PersonaServiceTests.cs
    │   │   ├── SemaphoreManagerTests.cs
    │   │   ├── ServiceIntegrationTests.cs
    │   │   ├── SupervisorServiceTests.cs
    │   │   └── WeatherServiceTests.cs
    │   ├── Usings.cs
    │   └── Utilities
    │       ├── MockObjects.cs
    │       └── TestDatabaseInitializer.cs
    ├── JiroBanner.png
    ├── Main.sln
    └── toc.yml
```
