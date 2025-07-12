using System.Text.Json;
using Jiro.Core.Commands.ComplexCommandResults;
using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;
using OpenAI.Chat;

namespace Jiro.Tests.SessionResultTests;

/// <summary>
/// Test to verify the JSON structure of SessionResult for getSessionHistory command
/// </summary>
public class SessionResultJsonTest
{
    [Fact]
    public void SessionResult_ShouldSerializeToExpectedJsonStructure()
    {
        // Arrange - Create a mock session with some messages
        var session = new Session
        {
            SessionId = "test-session-123",
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0),
            LastUpdatedAt = new DateTime(2024, 1, 1, 11, 30, 0),
            Messages = new List<ChatMessageWithMetadata>
            {
                new ChatMessageWithMetadata
                {
                    MessageId = "msg-1",
                    IsUser = true,
                    CreatedAt = new DateTime(2024, 1, 1, 10, 5, 0),
                    Type = MessageType.Text,
                    Message = ChatMessage.CreateUserMessage("Hello, how are you?")
                },
                new ChatMessageWithMetadata
                {
                    MessageId = "msg-2", 
                    IsUser = false,
                    CreatedAt = new DateTime(2024, 1, 1, 10, 6, 0),
                    Type = MessageType.Text,
                    Message = ChatMessage.CreateAssistantMessage("I'm doing well, thank you! How can I help you today?")
                }
            }
        };

        // Act - Create SessionResult and serialize to JSON (same as getSessionHistory command)
        var sessionResult = new SessionResult(session);
        var jsonData = JsonSerializer.Serialize(sessionResult);

        // Assert - Verify the JSON structure
        Assert.NotNull(jsonData);
        
        // Deserialize back to verify structure
        var deserializedResult = JsonSerializer.Deserialize<SessionResult>(jsonData);
        
        Assert.NotNull(deserializedResult);
        Assert.Equal("test-session-123", deserializedResult.SessionId);
        Assert.Equal(2, deserializedResult.Messages.Count);
        
        // Verify first message (user)
        var firstMessage = deserializedResult.Messages[0];
        Assert.Equal("msg-1", firstMessage.Id);
        Assert.True(firstMessage.IsUser);
        Assert.Equal("Hello, how are you?", firstMessage.Content);
        Assert.Equal(MessageType.Text, firstMessage.Type);
        
        // Verify second message (assistant)
        var secondMessage = deserializedResult.Messages[1];
        Assert.Equal("msg-2", secondMessage.Id);
        Assert.False(secondMessage.IsUser);
        Assert.Equal("I'm doing well, thank you! How can I help you today?", secondMessage.Content);
        Assert.Equal(MessageType.Text, secondMessage.Type);
        
        // Output the actual JSON for debugging
        Console.WriteLine("Generated JSON from getSessionHistory:");
        Console.WriteLine(jsonData);
    }
}
