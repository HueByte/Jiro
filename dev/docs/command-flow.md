# Command Flow Architecture

## Overview

Jiro implements a sophisticated command execution pipeline that handles command processing from reception through completion. This document details the complete command flow, from initial WebSocket reception through gRPC result transmission, including all intermediate processing steps and error handling mechanisms.

### Key Updates (v1.0.0-beta)
- **Client-side session ID generation**: Sessions are now created on the client side
- **Enhanced error handling**: Improved command synchronization and error recovery
- **Service separation**: SessionManager and MessageCacheService now handle session/message operations
- **Improved WebSocket contracts**: Using `IJiroInstance` interface for better type safety

## Command Flow Pipeline

### High-Level Flow

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2"
  }
}}%%
flowchart TB
    subgraph "Jiro Cloud Server"
        CloudCmd[Command Dispatch]
        CloudGrpc[gRPC Result Reception]
    end
    
    subgraph "Jiro Client - Reception Layer"
        WSHub[SignalR Hub]
        WSConn[WebSocketConnection]
        WSService[JiroWebSocketService]
    end
    
    subgraph "Jiro Client - Processing Layer"
        Scope[Service Scope Creation]
        Context[Command Context Setup]
        Handler[Command Handler]
        Execution[Command Execution]
    end
    
    subgraph "Jiro Client - Response Layer"
        ResultProc[Result Processing]
        GrpcService[JiroGrpcService]
        GrpcClient[gRPC Client]
    end
    
    CloudCmd -->|WebSocket| WSHub
    WSHub --> WSConn
    WSConn --> WSService
    
    WSService --> Scope
    Scope --> Context
    Context --> Handler
    Handler --> Execution
    
    Execution --> ResultProc
    ResultProc --> GrpcService
    GrpcService --> GrpcClient
    GrpcClient -->|gRPC Response| CloudGrpc
    
    %% High Contrast Styling
    classDef cloudStyle fill:#9C27B0,stroke:#4A148C,stroke-width:2px,color:#FFFFFF
    classDef receptionStyle fill:#2196F3,stroke:#0D47A1,stroke-width:2px,color:#FFFFFF
    classDef processingStyle fill:#FF9800,stroke:#E65100,stroke-width:2px,color:#000000
    classDef responseStyle fill:#4CAF50,stroke:#1B5E20,stroke-width:2px,color:#FFFFFF
    
    class CloudCmd,CloudGrpc cloudStyle
    class WSHub,WSConn,WSService receptionStyle
    class Scope,Context,Handler,Execution processingStyle
    class ResultProc,GrpcService,GrpcClient responseStyle
```

## Detailed Command Execution Flow

### 1. Command Reception Phase

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2",
    "actorBkg": "#E3F2FD",
    "actorBorder": "#1565C0",
    "actorTextColor": "#000000",
    "activationBkgColor": "#FFF3E0",
    "activationBorderColor": "#E65100",
    "noteBkgColor": "#FFFDE7",
    "noteBorderColor": "#F57C00",
    "noteTextColor": "#000000"
  }
}}%%
sequenceDiagram
    participant Server as Jiro Cloud
    participant SignalR as SignalR Hub
    participant WSConn as WebSocketConnection
    participant WSService as JiroWebSocketService
    participant Queue as Command Queue
    
    Server->>SignalR: Send Command Message
    Note over SignalR: JSON Deserialization<br/>CommandMessage Object
    
    SignalR->>WSConn: CommandReceived Event
    Note over WSConn: Event Handler Setup<br/>via JiroClientBase
    
    WSConn->>WSService: HandleCommandAsync()
    Note over WSService: Command Validation<br/>Queue Monitoring
    
    WSService->>Queue: Add Command (CommandSyncId)
    Note over Queue: Active Commands Tracking<br/>Metrics Increment
```

#### Command Message Structure

The incoming command follows this structure:

```typescript
interface CommandMessage {
  instanceId: string;        // Target Jiro instance identifier
  command: string;           // Command text to execute
  commandSyncId: string;     // Unique synchronization identifier
  sessionId: string;         // Chat session identifier (client-generated in v1.0.0-beta)
  parameters: Record<string, string>; // Additional command parameters
}
```

### 2. Service Scope and Context Setup

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2"
  }
}}%%
flowchart TD
    Start[Command Received] --> CreateScope[Create AsyncServiceScope]
    CreateScope --> ResolveContext[Resolve ICommandContext]
    ResolveContext --> ResolveHandler[Resolve ICommandHandlerService]
    ResolveHandler --> ResolveGrpc[Resolve IJiroGrpcService]
    
    ResolveGrpc --> SetInstance[Set Current Instance ID]
    SetInstance --> SetSession[Set Session ID]
    SetSession --> SetParams[Set Command Parameters]
    SetParams --> Ready[Context Ready for Execution]
    
    %% High Contrast Styling
    classDef setupStyle fill:#2196F3,stroke:#0D47A1,stroke-width:2px,color:#FFFFFF
    classDef resolveStyle fill:#FF9800,stroke:#E65100,stroke-width:2px,color:#000000
    classDef configStyle fill:#4CAF50,stroke:#1B5E20,stroke-width:2px,color:#FFFFFF
    
    class CreateScope setupStyle
    class ResolveContext,ResolveHandler,ResolveGrpc resolveStyle
    class SetInstance,SetSession,SetParams,Ready configStyle
```

#### Context Configuration Details

```csharp
// Command context setup in JiroWebSocketService
commandContext.SetCurrentInstance(commandMessage.InstanceId);
commandContext.SetSessionId(commandMessage.SessionId);
commandContext.SetData(commandMessage.Parameters.Select(kvp =>
    new KeyValuePair<string, object>(kvp.Key, kvp.Value)));
```

### 3. Command Execution Phase

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2",
    "actorBkg": "#E3F2FD",
    "actorBorder": "#1565C0",
    "actorTextColor": "#000000",
    "activationBkgColor": "#FFF3E0",
    "activationBorderColor": "#E65100",
    "noteBkgColor": "#FFFDE7",
    "noteBorderColor": "#F57C00",
    "noteTextColor": "#000000"
  }
}}%%
sequenceDiagram
    participant WSService as JiroWebSocketService
    participant Context as ICommandContext
    participant Handler as ICommandHandlerService
    participant Parser as Command Parser
    participant Executor as Command Executor
    participant Plugins as Plugin Commands
    
    WSService->>Handler: ExecuteCommandAsync(scope, command)
    Handler->>Parser: Parse Command String
    Parser->>Handler: Command Metadata
    
    Handler->>Context: Get Current Session/Instance
    Context->>Handler: Context Data
    
    Handler->>Executor: Route to Command Executor
    
    alt Built-in Command
        Executor->>Executor: Execute Core Command
    else Plugin Command
        Executor->>Plugins: Execute Plugin Command
        Plugins->>Executor: Plugin Result
    end
    
    Executor->>Handler: Command Result
    Handler->>WSService: CommandResponse Object
    
    Note over WSService: Result Contains:<br/>• Command Name<br/>• Command Type<br/>• Success Status<br/>• Result Data
```

#### Command Processing Pipeline

1. **Command Parsing**: Text command parsed into executable format
2. **Command Resolution**: Determine target command handler (core vs plugin)
3. **Context Injection**: Current session and instance data provided
4. **Command Execution**: Actual business logic execution
5. **Result Formatting**: Output formatted for transmission

### 4. Result Processing and Transmission

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2",
    "actorBkg": "#E3F2FD",
    "actorBorder": "#1565C0",
    "actorTextColor": "#000000",
    "activationBkgColor": "#FFF3E0",
    "activationBorderColor": "#E65100",
    "noteBkgColor": "#FFFDE7",
    "noteBorderColor": "#F57C00",
    "noteTextColor": "#000000"
  }
}}%%
sequenceDiagram
    participant WSService as JiroWebSocketService
    participant GrpcService as JiroGrpcService
    participant Context as ICommandContext
    participant ProtoClient as gRPC Client
    participant Server as Jiro Cloud
    
    WSService->>Context: Get Final Session ID
    Context->>WSService: Updated Session ID
    
    alt Successful Execution
        WSService->>GrpcService: SendCommandResultAsync()
        Note over GrpcService: Convert to ClientMessage<br/>Protobuf Format
    else Failed Execution
        WSService->>GrpcService: SendCommandErrorAsync()
        Note over GrpcService: Create Error Response<br/>Protobuf Format
    end
    
    GrpcService->>GrpcService: CreateMessage()
    Note over GrpcService: Data Type Detection:<br/>• Text/JSON<br/>• Graph Data<br/>• Error Response
    
    GrpcService->>ProtoClient: SendCommandResultAsync()
    
    loop Retry Logic (Max 3 attempts)
        ProtoClient->>Server: gRPC Call with ClientMessage
        Server->>ProtoClient: CommandResultResponse
        
        alt Success Response
            Note over ProtoClient: Operation Complete
        else Error Response
            Note over ProtoClient: Exponential Backoff<br/>Retry Attempt
        end
    end
    
    ProtoClient->>GrpcService: Final Result
    GrpcService->>WSService: Transmission Complete
```

#### Protobuf Message Structure

The result is converted to this protobuf structure:

```protobuf
message ClientMessage {
    string commandSyncId = 6;
    string commandName = 1;
    string sessionId = 8;
    DataType dataType = 2;
    bool isSuccess = 5;
    oneof result {
        TextResult textResult = 3;
        GraphResult graphResult = 4;
    }
}

enum DataType {
    TEXT = 0;
    GRAPH = 1;
}

message TextResult {
    string response = 1;
    TextType textType = 2;
}

enum TextType {
    PLAIN = 0;
    JSON = 1;
    BASE64 = 2;
    MARKDOWN = 3;
    HTML = 4;
}
```

### 5. Resource Cleanup and Monitoring

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2"
  }
}}%%
flowchart TD
    Complete[Command Execution Complete] --> UpdateMetrics[Update Command Metrics]
    UpdateMetrics --> RemoveFromQueue[Remove from Active Commands]
    RemoveFromQueue --> DisposeScope[Dispose Service Scope]
    
    DisposeScope --> CleanupContext[Cleanup Command Context]
    CleanupContext --> CleanupGrpc[Cleanup gRPC Service]
    CleanupGrpc --> FinalMetrics[Final Metrics Update]
    
    FinalMetrics --> LogResult[Log Command Completion]
    LogResult --> Ready[Ready for Next Command]
    
    %% Horizon Theme Styling
    classDef cleanupStyle fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    classDef metricsStyle fill:#26BBD9,stroke:#1A9CB8,stroke-width:2px,color:#06060C
    classDef completeStyle fill:#27D797,stroke:#21BFC2,stroke-width:2px,color:#06060C
    
    class DisposeScope,CleanupContext,CleanupGrpc cleanupStyle
    class UpdateMetrics,RemoveFromQueue,FinalMetrics metricsStyle
    class LogResult,Ready completeStyle
```

## Error Handling Flow

### Exception Processing Pipeline

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2"
  }
}}%%
flowchart TD
    Exception[Exception Thrown] --> Catch[Exception Caught]
    Catch --> LogError[Log Error Details]
    LogError --> CreateErrorScope[Create Error Scope]
    CreateErrorScope --> GetSessionId[Get Final Session ID]
    GetSessionId --> SendError[Send Error via gRPC]
    SendError --> UpdateFailedCount[Update Failed Commands Count]
    UpdateFailedCount --> CleanupError[Cleanup Resources]
    CleanupError --> RemoveFromQueue[Remove from Active Commands]
    
    %% Error types
    Catch --> ParseError{Command Parse Error?}
    Catch --> ExecutionError{Execution Error?}
    Catch --> GrpcError{gRPC Send Error?}
    
    ParseError -->|Yes| SendError
    ExecutionError -->|Yes| SendError
    GrpcError -->|Yes| LogGrpcFailure[Log gRPC Failure]
    LogGrpcFailure --> UpdateFailedCount
    
    %% Horizon Theme Styling
    classDef errorStyle fill:#E95378,stroke:#C7455C,stroke-width:2px,color:#FCD4B8
    classDef processStyle fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    classDef cleanupStyle fill:#26BBD9,stroke:#1A9CB8,stroke-width:2px,color:#06060C
    
    class Exception,ParseError,ExecutionError,GrpcError errorStyle
    class Catch,LogError,SendError processStyle
    class UpdateFailedCount,CleanupError,RemoveFromQueue cleanupStyle
```

### Error Response Structure

```csharp
// Error response creation in JiroGrpcService
var errorResult = new CommandResponse
{
    CommandName = "Error",
    CommandType = Jiro.Commands.CommandType.Text,
    IsSuccess = false,
    Result = Jiro.Commands.Results.TextResult.Create(errorMessage)
};
```

## Command Queue Monitoring

### Real-time Metrics Tracking

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2"
  }
}}%%
graph TB
    subgraph "Command Metrics"
        ActiveCount[Active Command Count]
        TotalProcessed[Total Commands Processed]
        SuccessfulCount[Successful Commands]
        FailedCount[Failed Commands]
    end
    
    subgraph "Queue Operations"
        AddCommand[Command Started]
        RemoveCommand[Command Completed]
        IncrementSuccess[Success Counter++]
        IncrementFailed[Failed Counter++]
    end
    
    subgraph "Monitoring Interface"
        Monitor[ICommandQueueMonitor]
        ActiveIds[Active Command IDs]
        Metrics[Real-time Metrics]
    end
    
    AddCommand --> ActiveCount
    RemoveCommand --> ActiveCount
    IncrementSuccess --> SuccessfulCount
    IncrementFailed --> FailedCount
    
    ActiveCount --> Monitor
    TotalProcessed --> Monitor
    SuccessfulCount --> Monitor
    FailedCount --> Monitor
    
    Monitor --> ActiveIds
    Monitor --> Metrics
    
    %% Horizon Theme Styling
    classDef metricsStyle fill:#26BBD9,stroke:#1A9CB8,stroke-width:2px,color:#06060C
    classDef operationsStyle fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    classDef interfaceStyle fill:#27D797,stroke:#21BFC2,stroke-width:2px,color:#06060C
    
    class ActiveCount,TotalProcessed,SuccessfulCount,FailedCount metricsStyle
    class AddCommand,RemoveCommand,IncrementSuccess,IncrementFailed operationsStyle
    class Monitor,ActiveIds,Metrics interfaceStyle
```

### Metrics Implementation

```csharp
// Command queue monitoring in JiroWebSocketService
private readonly ConcurrentDictionary<string, DateTime> _activeCommands = new();
private long _totalCommandsProcessed = 0;
private long _successfulCommands = 0;
private long _failedCommands = 0;

// Metrics properties
public int ActiveCommandCount => _activeCommands.Count;
public IEnumerable<string> ActiveCommandIds => _activeCommands.Keys;
public long TotalCommandsProcessed => _totalCommandsProcessed;
public long SuccessfulCommands => _successfulCommands;
public long FailedCommands => _failedCommands;
```

## Concurrency and Threading

### Thread Safety Considerations

```mermaid
%%{init: {
  "theme": "base",
  "themeVariables": {
    "background": "#FFFFFF",
    "primaryColor": "#2E7D32",
    "primaryTextColor": "#000000",
    "primaryBorderColor": "#1B5E20",
    "lineColor": "#424242",
    "sectionBkgColor": "#E8F5E9",
    "altSectionBkgColor": "#C8E6C9",
    "gridColor": "#E0E0E0",
    "secondaryColor": "#1976D2",
    "tertiaryColor": "#7B1FA2"
  }
}}%%
graph TB
    subgraph "Thread-Safe Components"
        ConcurrentDict[ConcurrentDictionary<br/>Active Commands]
        InterlockedCounters[Interlocked Counters<br/>Command Metrics]
        ScopedServices[Scoped Services<br/>Per Command]
    end
    
    subgraph "Threading Patterns"
        AsyncAwait[Async/Await Pattern]
        TaskRun[Task.Run for Background]
        Semaphores[Semaphores for Connection]
    end
    
    subgraph "Isolation Mechanisms"
        ServiceScope[Service Scope Isolation]
        CommandContext[Command Context Isolation]
        GrpcScoped[Scoped gRPC Services]
    end
    
    ConcurrentDict --> ServiceScope
    InterlockedCounters --> CommandContext
    ScopedServices --> GrpcScoped
    
    AsyncAwait --> ServiceScope
    TaskRun --> CommandContext
    Semaphores --> GrpcScoped
    
    %% Horizon Theme Styling
    classDef threadSafeStyle fill:#27D797,stroke:#21BFC2,stroke-width:2px,color:#06060C
    classDef patternStyle fill:#26BBD9,stroke:#1A9CB8,stroke-width:2px,color:#06060C
    classDef isolationStyle fill:#FCD4B8,stroke:#E29A6B,stroke-width:2px,color:#06060C
    
    class ConcurrentDict,InterlockedCounters,ScopedServices threadSafeStyle
    class AsyncAwait,TaskRun,Semaphores patternStyle
    class ServiceScope,CommandContext,GrpcScoped isolationStyle
```

### Concurrent Command Processing

- **Multiple Commands**: Can process multiple commands simultaneously
- **Thread Isolation**: Each command gets its own service scope and context
- **Resource Safety**: Thread-safe collections and atomic operations
- **Connection Management**: Semaphores protect WebSocket connection operations

## Performance Characteristics

### Processing Metrics

| Metric | Typical Value | Notes |
|--------|---------------|-------|
| Command Reception Latency | < 10ms | WebSocket to service handler |
| Service Scope Creation | < 5ms | Dependency injection overhead |
| Command Execution Time | Variable | Depends on command complexity |
| gRPC Transmission Time | 10-100ms | Network dependent |
| Resource Cleanup Time | < 5ms | Service scope disposal |

### Scalability Considerations

1. **Memory Usage**: Scoped services prevent memory leaks
2. **Connection Limits**: Single WebSocket, multiple gRPC calls
3. **Command Throughput**: Limited by command execution time
4. **Error Recovery**: Automatic retry and reconnection mechanisms
5. **Resource Management**: Proper disposal patterns throughout

## Integration Points

### Command Handler Integration

```csharp
// Custom command handler registration
services.RegisterCommands(nameof(ChatCommand.Chat));

// Command execution flow
var result = await _commandHandler.ExecuteCommandAsync(
    scope.ServiceProvider, 
    commandMessage.Command);
```

### Context Provider Integration

```csharp
// Command context setup
commandContext.SetCurrentInstance(commandMessage.InstanceId);
commandContext.SetSessionId(commandMessage.SessionId);
commandContext.SetData(commandMessage.Parameters.Select(kvp =>
    new KeyValuePair<string, object>(kvp.Key, kvp.Value)));
```

### gRPC Service Integration

```csharp
// gRPC client configuration
services.AddGrpcClient<JiroHubProtoClient>("JiroClient", options =>
{
    options.Address = new Uri(jiroCloudOptions.Grpc.ServerUrl);
})
.AddCallCredentials((context, metadata) =>
{
    metadata.Add("X-Api-Key", jiroCloudOptions.ApiKey);
    return Task.CompletedTask;
});
```

## Best Practices

### Command Flow Optimization

1. **Minimize Scope Lifetime**: Create and dispose service scopes quickly
2. **Async All the Way**: Use async/await throughout the pipeline
3. **Error Boundary**: Catch exceptions at the service boundary
4. **Resource Cleanup**: Always dispose scoped services properly
5. **Monitoring Integration**: Track metrics at key pipeline points

### Error Handling Guidelines

1. **Graceful Degradation**: Continue processing other commands on failures
2. **Comprehensive Logging**: Log at each major pipeline stage
3. **Retry Logic**: Implement exponential backoff for transient failures
4. **Context Preservation**: Maintain command context through error flows
5. **Cleanup Guarantee**: Ensure resources are cleaned up even on errors

This command flow architecture provides a robust, scalable, and maintainable pipeline for processing commands in the Jiro application, with comprehensive error handling, monitoring, and resource management capabilities.
