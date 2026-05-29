using HomeMcp.Application.Memory.Commands.ForgetFact;
using HomeMcp.Application.Memory.Commands.RememberFact;
using HomeMcp.Application.Orchestration;
using HomeMcp.Application.Sessions.Commands.ProcessTurn;
using HomeMcp.Application.Sessions.Commands.StartSession;
using HomeMcp.Application.Sessions.Queries.GetSession;

namespace HomeMcp.Server.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<StartSessionCommandHandler>();
        services.AddScoped<ProcessTurnCommandHandler>();
        services.AddScoped<GetSessionQueryHandler>();
        services.AddScoped<RememberFactCommandHandler>();
        services.AddScoped<ForgetFactCommandHandler>();
        services.AddSingleton<PromptBuilder>();
        return services;
    }
}
