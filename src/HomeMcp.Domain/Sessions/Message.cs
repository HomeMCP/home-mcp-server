using CSharpFunctionalExtensions;
using HomeMcp.Domain.Sessions.Errors;

namespace HomeMcp.Domain.Sessions;

public class Message : HomeMcp.Domain.SharedKernel.Entity<MessageId>
{
    private Message(
        MessageId id,
        SessionId sessionId,
        MessageRole role,
        string content,
        Maybe<string> toolCallsJson,
        DateTimeOffset createdAt) : base(id)
    {
        SessionId = sessionId;
        Role = role;
        Content = content;
        ToolCallsJson = toolCallsJson;
        CreatedAt = createdAt;
    }

    public SessionId SessionId { get; }
    public MessageRole Role { get; }
    public string Content { get; }
    public Maybe<string> ToolCallsJson { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Result<Message, MessageError> CreateUser(
        SessionId sessionId,
        string content,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return MessageErrors.EmptyContent("User");
        }

        return new Message(new MessageId(0), sessionId, MessageRole.User, content, Maybe<string>.None, timeProvider.GetUtcNow());
    }

    public static Result<Message, MessageError> CreateAssistant(
        SessionId sessionId,
        string content,
        TimeProvider timeProvider,
        Maybe<string> toolCallsJson = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return MessageErrors.EmptyContent("Assistant");
        }

        return new Message(new MessageId(0), sessionId, MessageRole.Assistant, content, toolCallsJson, timeProvider.GetUtcNow());
    }

    public static Result<Message, MessageError> CreateTool(
        SessionId sessionId,
        string content,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return MessageErrors.EmptyToolResult();
        }

        return new Message(new MessageId(0), sessionId, MessageRole.Tool, content, Maybe<string>.None, timeProvider.GetUtcNow());
    }

    public static Message Reconstitute(
        MessageId id,
        SessionId sessionId,
        MessageRole role,
        string content,
        Maybe<string> toolCallsJson,
        DateTimeOffset createdAt) =>
        new(id, sessionId, role, content, toolCallsJson, createdAt);
}
