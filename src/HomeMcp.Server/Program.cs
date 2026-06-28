using HomeMcp.Application.Telemetry;
using HomeMcp.Domain.Plugins;
using HomeMcp.Infrastructure.DependencyInjection;
using HomeMcp.Infrastructure.McpHost;
using HomeMcp.Infrastructure.Persistence;
using HomeMcp.Plugin.Jellyfin;
using HomeMcp.Server.Admin;
using HomeMcp.Server.DependencyInjection;
using HomeMcp.Server.GrpcServices;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var sqlitePath = builder.Configuration["Storage:Sqlite:Path"] ?? "./data/home-mcp.db";
var ollamaUrl = builder.Configuration["Llm:OllamaUrl"] ?? "http://localhost:11434";
var chatModel = builder.Configuration["Llm:ChatModel"] ?? "qwen2.5:7b-instruct";

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sqlitePath))!);

builder.Services.AddInfrastructure(new InfrastructureOptions(
    $"Data Source={sqlitePath}",
    ollamaUrl,
    chatModel));

builder.Services.AddApplicationServices();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(HomeMcpTelemetry.ServiceName))
    .WithTracing(t => t
        .AddSource(HomeMcpTelemetry.ServiceName)
        .AddAspNetCoreInstrumentation(o => o.RecordException = true)
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter(HomeMcpTelemetry.ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
    o.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(HomeMcpTelemetry.ServiceName));
    o.AddOtlpExporter();
});

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddControllers();
builder.Services.AddSingleton<AdminLogBuffer>();
builder.Services.AddSingleton<SetupState>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("admin", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201")
            .AllowAnyHeader()
            .AllowAnyMethod());

    options.AddPolicy("clients", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Logging.Services.AddSingleton<ILoggerProvider, AdminLoggerProvider>();

builder.Services.AddSingleton<JellyfinPlugin>();
// builder.Services.AddSingleton<HomeAssistantPlugin>();

builder.Services.AddSingleton<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    var jellyfin = cfg.GetSection("Plugins:Jellyfin");
    if (jellyfin.Exists())
    {
        result["io.jellyfin"] = jellyfin.GetChildren().ToDictionary(c => c.Key, c => c.Value ?? string.Empty);
    }

    var ha = cfg.GetSection("Plugins:HomeAssistant");
    if (ha.Exists())
    {
        result["io.homeassistant"] = ha.GetChildren().ToDictionary(c => c.Key, c => c.Value ?? string.Empty);
    }

    return result;
});

builder.Services.AddSingleton<IEnumerable<IPlugin>>(sp => [
    sp.GetRequiredService<JellyfinPlugin>()
]);

builder.Services.AddHostedService<PluginInitializationService>();
builder.Services.AddScoped<AssistantService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
    migrator.Migrate();
}

app.UseCors("admin");
app.MapControllers();
app.MapGrpcService<AssistantService>();
app.MapGrpcReflectionService();
app.MapGet("/", () => "HomeMcp gRPC server is running.");

app.Run();
