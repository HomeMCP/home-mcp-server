using HomeMcp.Domain.Plugins;
using HomeMcp.Infrastructure.DependencyInjection;
using HomeMcp.Infrastructure.McpHost;
using HomeMcp.Infrastructure.Persistence;
using HomeMcp.Plugin.Jellyfin;
using HomeMcp.Server.Admin;
using HomeMcp.Server.DependencyInjection;
using HomeMcp.Server.GrpcServices;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
var sqlitePath = builder.Configuration["Storage:Sqlite:Path"] ?? "./data/home-mcp.db";
var ollamaUrl = builder.Configuration["Llm:OllamaUrl"] ?? "http://localhost:11434";
var chatModel = builder.Configuration["Llm:ChatModel"] ?? "qwen2.5:7b-instruct";

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sqlitePath))!);

// ── Core services ─────────────────────────────────────────────────────────────
builder.Services.AddInfrastructure(new InfrastructureOptions(
    $"Data Source={sqlitePath}",
    ollamaUrl,
    chatModel));

builder.Services.AddApplicationServices();

// ── gRPC ──────────────────────────────────────────────────────────────────────
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// ── REST / Admin API ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSingleton<AdminLogBuffer>();
builder.Services.AddSingleton<SetupState>();
builder.Services.AddCors(options =>
{
    // Admin UI (internal): only Angular dev-server origins
    options.AddPolicy("admin", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201")
            .AllowAnyHeader()
            .AllowAnyMethod());

    // Client pairing API (external): open for device clients
    options.AddPolicy("clients", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Logging.Services.AddSingleton<ILoggerProvider, AdminLoggerProvider>();

// ── Plugins ──────────────────────────────────────────────────────────────────
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

// ── Build app ─────────────────────────────────────────────────────────────────
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
