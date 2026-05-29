using DbUp;
using Microsoft.Extensions.Logging;

namespace HomeMcp.Infrastructure.Persistence;

public sealed class DatabaseMigrator
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(string connectionString, ILogger<DatabaseMigrator> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public void Migrate()
    {
        var upgrader = DeployChanges.To
            .SqliteDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .LogToConsole()
            .Build();

        if (upgrader.IsUpgradeRequired())
        {
            _logger.LogInformation("Running database migrations...");
            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                _logger.LogError(result.Error, "Database migration failed.");
                throw result.Error;
            }

            _logger.LogInformation("Database migration completed.");
        }
    }
}
