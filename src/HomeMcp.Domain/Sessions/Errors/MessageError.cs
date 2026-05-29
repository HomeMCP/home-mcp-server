using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Sessions.Errors;

public abstract record MessageError(string Code, string Message) : DomainError(Code, Message);

public sealed record MessageEmptyContentError(string Role)
    : MessageError(MessageErrors.Codes.EmptyContent, $"{Role} message cannot be empty."), IValidationError;

public sealed record MessageEmptyToolResultError()
    : MessageError(MessageErrors.Codes.EmptyToolResult, "Tool result cannot be empty."), IValidationError;

public static class MessageErrors
{
    public static class Codes
    {
        public const string EmptyContent = "message.empty_content";
        public const string EmptyToolResult = "message.empty_tool_result";
        public const string Invalid = "message.invalid";
    }

    public static MessageEmptyContentError EmptyContent(string role) => new(role);
    public static MessageEmptyToolResultError EmptyToolResult() => new();
}
