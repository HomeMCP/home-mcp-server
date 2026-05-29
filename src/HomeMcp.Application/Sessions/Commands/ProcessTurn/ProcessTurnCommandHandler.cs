using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Application.Orchestration;
using HomeMcp.Application.Plugins;
using HomeMcp.Domain.Memory;
using HomeMcp.Domain.Sessions;
using HomeMcp.Domain.Sessions.Errors;

namespace HomeMcp.Application.Sessions.Commands.ProcessTurn;

public sealed class ProcessTurnCommandHandler
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IFactRepository _factRepo;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IChatOrchestrator _orchestrator;
    private readonly PromptBuilder _promptBuilder;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _timeProvider;

    public ProcessTurnCommandHandler(
        ISessionRepository sessionRepo,
        IFactRepository factRepo,
        IPluginRegistry pluginRegistry,
        IChatOrchestrator orchestrator,
        PromptBuilder promptBuilder,
        IUnitOfWork uow,
        TimeProvider timeProvider)
    {
        _sessionRepo = sessionRepo;
        _factRepo = factRepo;
        _pluginRegistry = pluginRegistry;
        _orchestrator = orchestrator;
        _promptBuilder = promptBuilder;
        _uow = uow;
        _timeProvider = timeProvider;
    }

    public async IAsyncEnumerable<TurnEvent> HandleAsync(
        ProcessTurnCommand command,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var sessionId = SessionId.From(command.SessionId);
        var sessionMaybe = await _sessionRepo.GetByIdAsync(sessionId, ct);

        if (sessionMaybe.HasNoValue)
        {
            yield return new TurnErrorEvent(ApplicationError.NotFound("session", command.SessionId));
            yield break;
        }

        var session = sessionMaybe.Value;

        if (string.IsNullOrWhiteSpace(command.UserInput))
        {
            yield return new TurnErrorEvent(
                ApplicationError.Validation(SessionErrors.Codes.EmptyInput, "User input cannot be empty."));
            yield break;
        }

        var plugins = _pluginRegistry.GetAll();
        var facts = await _factRepo.GetByUserAsync(session.UserId, null, ct);
        var systemPrompt = _promptBuilder.Build(session, plugins, facts, session.Locale);

        var history = session.GetMessagesAfterSummary()
            .Select(m => new HistoryMessage(m.Role.ToString().ToLowerInvariant(), m.Content, m.ToolCallsJson))
            .ToList();

        var context = new OrchestratorContext(systemPrompt, history, command.UserInput, plugins);

        var fullResponse = new System.Text.StringBuilder();

        await foreach (var turnEvent in _orchestrator.OrchestrateAsync(context, ct))
        {
            if (turnEvent is TurnCompletedEvent completed)
            {
                fullResponse.Append(completed.FullResponse);
            }

            yield return turnEvent;
        }

        var userMsgResult = Message.CreateUser(sessionId, command.UserInput, _timeProvider);
        var assistantMsgResult = Message.CreateAssistant(sessionId, fullResponse.ToString(), _timeProvider);

        if (userMsgResult.IsFailure || assistantMsgResult.IsFailure)
        {
            yield return new TurnErrorEvent(
                ApplicationError.Validation(MessageErrors.Codes.Invalid, "Could not create message records."));
            yield break;
        }

        var userMsg = userMsgResult.Value;
        var assistantMsg = assistantMsgResult.Value;

        session.Touch();

        await _uow.ExecuteAsync(async saveCt =>
        {
            var r1 = await _sessionRepo.AddMessageAsync(sessionId, userMsg, saveCt);
            if (r1.IsFailure)
            {
                return r1.Error.ToApplicationError();
            }

            var r2 = await _sessionRepo.AddMessageAsync(sessionId, assistantMsg, saveCt);
            if (r2.IsFailure)
            {
                return r2.Error.ToApplicationError();
            }

            var r3 = await _sessionRepo.UpdateAsync(session, saveCt);
            return r3.IsSuccess
                ? CSharpFunctionalExtensions.UnitResult.Success<ApplicationError>()
                : r3.Error.ToApplicationError();
        }, ct);
    }
}
