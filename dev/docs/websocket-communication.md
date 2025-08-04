# WebSocket Communication Architecture

## Overview

Jiro implements a hybrid communication architecture that combines WebSocket connections for receiving commands and gRPC for sending command results. This design provides real-time command reception through SignalR WebSockets while ensuring reliable command result delivery via gRPC with retry mechanisms.

## Architecture Components

### Core Services

- **`JiroWebSocketService`**: Main orchestration service managing the entire communication lifecycle
- **`SignalRWebSocketConnection`**: WebSocket implementation using SignalR for real-time command reception
- **`JiroGrpcService`**: gRPC client service for sending command results back to the server
- **`ICommandQueueMonitor`**: Interface for monitoring command execution metrics

## Communication Flow

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#1C1E26",
    "primaryColor": "#FCD4B8",
    "primaryTextColor": "#D5D8DA",
    "primaryBorderColor": "#E95378",
    "lineColor": "#6C6F93",
    "sectionBkgColor": "#232530",
    "altSectionBkgColor": "#2E303E",
    "gridColor": "#16161C",
    "secondaryColor": "#26BBD9",
    "tertiaryColor": "#27D797",
    "actorBkg": "#2E303E",
    "actorBorder": "#6C6F93",
    "actorTextColor": "#D5D8DA",
    "activationBkgColor": "#FCD4B8",
    "activationBorderColor": "#E29A6B",
    "noteBkgColor": "#26BBD9",
    "noteBorderColor": "#1A9CB8",
    "noteTextColor": "#1C1E26",
    "altColor": "#27D797"
  }
}}%%
sequenceDiagram
    participant Server as Jiro Server
    participant WS as SignalR WebSocket
    participant WSService as JiroWebSocketService
    participant CmdHandler as Command Handler
    participant GrpcService as JiroGrpcService
    participant GrpcClient as gRPC Client

    Note over Server, GrpcClient: Command Execution Flow

    Server->>WS: Send Command (JSON)
    WS->>WSService: ReceiveCommand Event
    WSService->>WSService: Create Scope
    WSService->>CmdHandler: Execute Command
    CmdHandler->>WSService: Command Result
    WSService->>GrpcService: Send Result
    GrpcService->>GrpcClient: gRPC Call (with retry)
    GrpcClient->>Server: Command Response
    
    Note over WSService: Error Handling
    alt Command Execution Error
        CmdHandler-->>WSService: Exception
        WSService->>GrpcService: Send Error
        GrpcService->>GrpcClient: Error Response
    end
```

## Service Architecture

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#1C1E26",
    "primaryColor": "#FCD4B8",
    "primaryTextColor": "#D5D8DA",
    "primaryBorderColor": "#E95378",
    "lineColor": "#6C6F93",
    "sectionBkgColor": "#232530",
    "altSectionBkgColor": "#2E303E",
    "gridColor": "#16161C",
    "secondaryColor": "#26BBD9",
    "tertiaryColor": "#27D797",
    "cScale0": "#1C1E26",
    "cScale1": "#232530",
    "cScale2": "#2E303E",
    "cScale3": "#6C6F93",
    "cScale4": "#D5D8DA"
  }
}}%%
graph TB
    subgraph "Application Layer"
        App[Jiro Application]
        HSvc[Hosted Services]
    end
    
    subgraph "Communication Services"
        WSService[JiroWebSocketService<br/>Background Service]
        WSConn[SignalRWebSocketConnection<br/>IWebSocketConnection]
        GrpcSvc[JiroGrpcService<br/>IJiroGrpcService]
        Monitor[ICommandQueueMonitor]
    end
    
    subgraph "Core Services"
        CmdHandler[ICommandHandlerService]
        CmdContext[ICommandContext]
        ScopeFactory[IServiceScopeFactory]
    end
    
    subgraph "External Communication"
        SignalR[SignalR Hub<br/>WebSocket Server]
        GrpcServer[gRPC Server<br/>JiroHubProto]
    end
    
    App --> HSvc
    HSvc --> WSService
    WSService --> WSConn
    WSService --> GrpcSvc
    WSService --> CmdHandler
    WSService --> ScopeFactory
    WSService -.-> Monitor
    
    WSConn <--> SignalR
    GrpcSvc --> GrpcServer
    
    %% Horizon Theme Styling
    classDef wsService fill:#26BBD9,stroke:#1A9CB8,stroke-width:2px,color:#06060C
    classDef grpcService fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    classDef wsConn fill:#27D797,stroke:#21BFC2,stroke-width:2px,color:#06060C
    
    class WSService wsService
    class GrpcSvc grpcService
    class WSConn wsConn
```

## Dependency Injection and Scoping

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#1C1E26",
    "primaryColor": "#FCD4B8",
    "primaryTextColor": "#D5D8DA",
    "primaryBorderColor": "#E95378",
    "lineColor": "#6C6F93",
    "sectionBkgColor": "#232530",
    "altSectionBkgColor": "#2E303E",
    "gridColor": "#16161C",
    "secondaryColor": "#26BBD9",
    "tertiaryColor": "#27D797",
    "cScale0": "#1C1E26",
    "cScale1": "#232530",
    "cScale2": "#2E303E",
    "cScale3": "#6C6F93",
    "cScale4": "#D5D8DA",
    "clusterBkg": "#232530",
    "clusterBorder": "#6C6F93"
  }
}}%%
graph LR
    subgraph "Singleton Services"
        WSService[JiroWebSocketService]
        WSConn[SignalR Connection]
        Monitor[Command Queue Monitor]
    end
    
    subgraph "Scoped Services (Per Command)"
        GrpcSvc[JiroGrpcService]
        CmdContext[ICommandContext]
        GrpcClient[gRPC Client]
    end
    
    subgraph "Service Resolution"
        ScopeFactory[IServiceScopeFactory]
        Scope[AsyncServiceScope]
    end
    
    WSService --> ScopeFactory
    ScopeFactory --> Scope
    Scope --> GrpcSvc
    Scope --> CmdContext
    GrpcSvc --> GrpcClient
    
    WSService -.-> Monitor
    WSService --> WSConn
    
    %% Horizon Theme Styling
    classDef scopeStyle fill:#26BBD9,stroke:#1A9CB8,stroke-width:2px,color:#06060C
    classDef grpcStyle fill:#27D797,stroke:#21BFC2,stroke-width:2px,color:#06060C
    classDef wsStyle fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    
    class Scope scopeStyle
    class GrpcSvc grpcStyle
    class WSService wsStyle
```

## Message Flow and Data Structures

### Command Message Structure

```json
{
  "instanceId": "user-123",
  "command": "chat Hello Jiro",
  "commandSyncId": "cmd-456-789",
  "sessionId": "session-abc",
  "parameters": {
    "key1": "value1",
    "key2": "value2"
  }
}
```

### Command Response Structure (gRPC)

```protobuf
message ClientMessage {
    string commandName = 1;
    CommandType commandType = 2;
    oneof result {
        TextResult textResult = 3;
        GraphResult graphResult = 4;
    }
    bool isSuccess = 5;
    string commandSyncId = 6;
}
```

## Connection Management

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#1C1E26",
    "primaryColor": "#FCD4B8",
    "primaryTextColor": "#D5D8DA",
    "primaryBorderColor": "#E95378",
    "lineColor": "#6C6F93",
    "sectionBkgColor": "#232530",
    "altSectionBkgColor": "#2E303E",
    "gridColor": "#16161C",
    "secondaryColor": "#26BBD9",
    "tertiaryColor": "#27D797",
    "cScale0": "#1C1E26",
    "cScale1": "#232530",
    "cScale2": "#2E303E",
    "cScale3": "#6C6F93",
    "cScale4": "#D5D8DA",
    "stateLabelColor": "#D5D8DA"
  }
}}%%
stateDiagram-v2
    [*] --> Disconnected
    
    Disconnected --> Connecting: StartAsync()
    Connecting --> Connected: Connection Successful
    Connecting --> Failed: Connection Failed
    
    Connected --> Reconnecting: Connection Lost
    Connected --> Disconnecting: StopAsync()
    
    Reconnecting --> Connected: Reconnection Successful
    Reconnecting --> Failed: Max Retries Exceeded
    
    Failed --> Connecting: Retry Attempt
    Disconnecting --> Disconnected: Stop Complete
    
    note right of Connected
        - Receiving Commands
        - Sending Results
        - Monitoring Queue
    end note
    
    note right of Reconnecting
        - Exponential Backoff
        - Clear Active Commands
        - Event Notifications
    end note
```

## Error Handling and Retry Logic

### WebSocket Connection Retry

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#1C1E26",
    "primaryColor": "#FCD4B8",
    "primaryTextColor": "#D5D8DA",
    "primaryBorderColor": "#E95378",
    "lineColor": "#6C6F93",
    "sectionBkgColor": "#232530",
    "altSectionBkgColor": "#2E303E",
    "gridColor": "#16161C",
    "secondaryColor": "#26BBD9",
    "tertiaryColor": "#27D797",
    "cScale0": "#1C1E26",
    "cScale1": "#232530",
    "cScale2": "#2E303E",
    "cScale3": "#6C6F93",
    "cScale4": "#D5D8DA",
    "clusterBkg": "#232530",
    "clusterBorder": "#6C6F93"
  }
}}%%
flowchart TD
    Start[Connection Attempt] --> Try[Connect to Hub]
    Try --> Success{Connected?}
    Success -->|Yes| Monitor[Monitor Connection]
    Success -->|No| Retry{Max Retries?}
    Retry -->|No| Wait[Exponential Backoff]
    Wait --> Try
    Retry -->|Yes| Fail[Connection Failed]
    
    Monitor --> Lost{Connection Lost?}
    Lost -->|Yes| Auto[Auto Reconnect]
    Lost -->|No| Monitor
    Auto --> Try
    
    %% Horizon Theme Styling
    classDef successStyle fill:#27D797,stroke:#21BFC2,stroke-width:2px,color:#06060C
    classDef failStyle fill:#E95378,stroke:#C7455C,stroke-width:2px,color:#06060C
    classDef waitStyle fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    
    class Success successStyle
    class Fail failStyle
    class Wait waitStyle
```

### gRPC Result Sending Retry

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#1C1E26",
    "primaryColor": "#FCD4B8",
    "primaryTextColor": "#D5D8DA",
    "primaryBorderColor": "#E95378",
    "lineColor": "#6C6F93",
    "sectionBkgColor": "#232530",
    "altSectionBkgColor": "#2E303E",
    "gridColor": "#16161C",
    "secondaryColor": "#26BBD9",
    "tertiaryColor": "#27D797",
    "cScale0": "#1C1E26",
    "cScale1": "#232530",
    "cScale2": "#2E303E",
    "cScale3": "#6C6F93",
    "cScale4": "#D5D8DA",
    "clusterBkg": "#232530",
    "clusterBorder": "#6C6F93"
  }
}}%%
flowchart TD
    Send[Send gRPC Message] --> Attempt[gRPC Call]
    Attempt --> Response{Successful?}
    Response -->|Yes| Complete[Operation Complete]
    Response -->|No| RetryCheck{Retries Left?}
    RetryCheck -->|Yes| Backoff[Exponential Backoff]
    RetryCheck -->|No| Error[Throw Exception]
    Backoff --> Attempt
    
    %% Horizon Theme Styling
    classDef completeStyle fill:#27D797,stroke:#21BFC2,stroke-width:2px,color:#06060C
    classDef errorStyle fill:#E95378,stroke:#C7455C,stroke-width:2px,color:#06060C
    classDef backoffStyle fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    
    class Complete completeStyle
    class Error errorStyle
    class Backoff backoffStyle
```

## Configuration

### WebSocket Configuration

```json
{
  "WebSocket": {
    "HubUrl": "https://localhost:5001/instanceHub",
    "ReconnectionDelayMs": 5000,
    "MaxReconnectionAttempts": 5,
    "HandshakeTimeoutMs": 15000,
    "KeepAliveIntervalMs": 15000,
    "ServerTimeoutMs": 30000,
    "AccessToken": null,
    "Headers": {
      "User-Agent": "Jiro-Bot/1.0"
    }
  }
}
```

### gRPC Configuration

```json
{
  "Grpc": {
    "ServerUrl": "https://localhost:5001",
    "TimeoutMs": 30000,
    "MaxRetries": 3
  }
}
```

## Service Registration

The communication services are registered in the DI container as follows:

```csharp
// Configure options
services.Configure<WebSocketOptions>(configuration.GetSection("WebSocket"));
services.Configure<GrpcOptions>(configuration.GetSection("Grpc"));

// Register scoped gRPC service (per command execution)
services.AddScoped<IJiroGrpcService, JiroGrpcService>();

// Register singleton WebSocket connection
services.AddSingleton<IWebSocketConnection, SignalRWebSocketConnection>();

// Register main orchestration service as hosted service
services.AddHostedService<JiroWebSocketService>();

// Register command queue monitoring interface
services.AddSingleton<ICommandQueueMonitor, JiroWebSocketService>();
```

## Command Execution Lifecycle

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#1C1E26",
    "primaryColor": "#FCD4B8",
    "primaryTextColor": "#D5D8DA",
    "primaryBorderColor": "#E95378",
    "lineColor": "#6C6F93",
    "sectionBkgColor": "#232530",
    "altSectionBkgColor": "#2E303E",
    "gridColor": "#16161C",
    "secondaryColor": "#26BBD9",
    "tertiaryColor": "#27D797",
    "cScale0": "#1C1E26",
    "cScale1": "#232530",
    "cScale2": "#2E303E",
    "cScale3": "#6C6F93",
    "cScale4": "#D5D8DA",
    "actorBkg": "#2E303E",
    "actorBorder": "#6C6F93",
    "actorTextColor": "#D5D8DA",
    "activationBkgColor": "#FCD4B8",
    "activationBorderColor": "#E29A6B",
    "noteBkgColor": "#26BBD9",
    "noteBorderColor": "#1A9CB8",
    "noteTextColor": "#1C1E26"
  }
}}%%
sequenceDiagram
    participant WS as WebSocket
    participant Service as JiroWebSocketService
    participant Scope as Service Scope
    participant Context as Command Context
    participant Handler as Command Handler
    participant gRPC as gRPC Service

    WS->>Service: Command Received
    Service->>Service: Add to Active Commands
    Service->>Scope: Create Async Scope
    Scope->>Context: Resolve ICommandContext
    Scope->>gRPC: Resolve IJiroGrpcService
    
    Service->>Context: Set Instance/Session Data
    Service->>Handler: Execute Command
    Handler-->>Service: Command Result
    
    Service->>gRPC: Send Result
    gRPC-->>Service: Result Sent
    Service->>Service: Remove from Active Commands
    Service->>Service: Update Metrics
    
    Note over Scope: Scope Disposed<br/>Resources Cleaned Up
```

## Command Queue Monitoring

The `ICommandQueueMonitor` interface provides real-time insights into command execution:

### Monitoring Metrics

- **Active Command Count**: Number of currently executing commands
- **Active Command IDs**: List of command synchronization IDs being processed
- **Total Commands Processed**: Lifetime count of processed commands
- **Successful Commands**: Count of successfully completed commands
- **Failed Commands**: Count of commands that resulted in errors

### Usage Example

```csharp
public class MonitoringController : ControllerBase
{
    private readonly ICommandQueueMonitor _monitor;
    
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            ActiveCommands = _monitor.ActiveCommandCount,
            TotalProcessed = _monitor.TotalCommandsProcessed,
            SuccessRate = _monitor.SuccessfulCommands / (double)_monitor.TotalCommandsProcessed
        });
    }
}
```

## Key Benefits

### Architecture Advantages

1. **Real-time Communication**: WebSocket provides instant command delivery
2. **Reliable Result Delivery**: gRPC ensures command results reach the server
3. **Automatic Reconnection**: Built-in reconnection logic for WebSocket connections
4. **Retry Mechanisms**: Exponential backoff for both WebSocket and gRPC operations
5. **Proper Resource Management**: Scoped services ensure proper cleanup per command
6. **Monitoring and Observability**: Built-in command queue monitoring
7. **Error Resilience**: Comprehensive error handling at all levels

### Performance Characteristics

- **Low Latency**: Direct WebSocket connection for command reception
- **High Reliability**: gRPC with retry logic for result delivery
- **Resource Efficient**: Scoped dependency injection prevents resource leaks
- **Concurrent Processing**: Multiple commands can be processed simultaneously
- **Graceful Degradation**: System continues operating during temporary network issues

## Integration Points

### Server-Side Integration

The server must implement:

- SignalR Hub with `ReceiveCommand` method
- gRPC service implementing `JiroHubProto` service
- Proper authentication and authorization mechanisms

### Client Command Processing

Commands flow through the following pipeline:

1. WebSocket reception via SignalR
2. JSON deserialization to `CommandMessage`
3. Service scope creation for dependency injection
4. Command context setup with instance and session data
5. Command execution via `ICommandHandlerService`
6. Result serialization and gRPC transmission
7. Resource cleanup and metrics updating

This architecture provides a robust, scalable, and maintainable communication layer for the Jiro application.
