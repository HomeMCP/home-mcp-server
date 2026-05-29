namespace HomeMcp.Application.Sessions.Commands.ProcessTurn;

public sealed record ProcessTurnCommand(
    string SessionId,
    string UserInput);
