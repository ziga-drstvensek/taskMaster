using BacklogApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BacklogApi.Infrastructure.Services;

public class DatabaseBackupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly IConfiguration _configuration;
    private DateTime _lastBackupTime = DateTime.MinValue;
    private string _lastDatabaseHash = string.Empty;

    public DatabaseBackupService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseBackupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database Backup Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndBackupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database backup check.");
            }

            await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
        }
    }

    private async Task CheckAndBackupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BacklogDbContext>();

        var currentHash = await GetDatabaseHashAsync(context, cancellationToken);

        if (currentHash != _lastDatabaseHash)
        {
            _logger.LogInformation("Database changes detected. Creating backup...");
            await CreateBackupAsync(cancellationToken);
            _lastDatabaseHash = currentHash;
            _lastBackupTime = DateTime.UtcNow;
            _logger.LogInformation("Backup completed at {Time}", _lastBackupTime);
        }
        else
        {
            _logger.LogInformation("No database changes detected. Skipping backup.");
        }
    }

    private async Task<string> GetDatabaseHashAsync(BacklogDbContext context, CancellationToken cancellationToken)
    {
        var stats = new List<string>();

        try
        {
            var boardCount = await context.Boards.CountAsync(cancellationToken);
            var itemCount = await context.BacklogItems.CountAsync(cancellationToken);
            var columnCount = await context.Columns.CountAsync(cancellationToken);
            var commentCount = await context.Comments.CountAsync(cancellationToken);
            var sprintCount = await context.Sprints.CountAsync(cancellationToken);
            var userCount = await context.Users.CountAsync(cancellationToken);

            var lastItemUpdate = await context.BacklogItems
                .OrderByDescending(i => i.UpdatedAt)
                .Select(i => i.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            stats.Add($"boards:{boardCount}");
            stats.Add($"items:{itemCount}");
            stats.Add($"columns:{columnCount}");
            stats.Add($"comments:{commentCount}");
            stats.Add($"sprints:{sprintCount}");
            stats.Add($"users:{userCount}");
            stats.Add($"lastUpdate:{lastItemUpdate:O}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not calculate database hash, forcing backup.");
            return Guid.NewGuid().ToString();
        }

        var combined = string.Join("|", stats);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(combined));
    }

    private async Task CreateBackupAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var backupPath = Environment.GetEnvironmentVariable("BACKUP_PATH") ?? "/backups";

        if (!Directory.Exists(backupPath))
        {
            Directory.CreateDirectory(backupPath);
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFile = Path.Combine(backupPath, $"backlog_db_backup_{timestamp}.sql");

        var connParts = ParseConnectionString(connectionString ?? "");
        var server = connParts.GetValueOrDefault("server", "db");
        var database = connParts.GetValueOrDefault("database", "backlog_db");
        var user = connParts.GetValueOrDefault("user", "root");
        var password = connParts.GetValueOrDefault("password", "root");

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "mysqldump",
                Arguments = $"-h {server} -u {user} -p{password} {database}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                await File.WriteAllTextAsync(backupFile, output, cancellationToken);
                _logger.LogInformation("Backup saved to {BackupFile}", backupFile);

                await CleanupOldBackupsAsync(backupPath, 10);
            }
            else
            {
                _logger.LogError("mysqldump failed: {Error}", error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database backup.");
        }
    }

    private Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[keyValue[0].Trim()] = keyValue[1].Trim();
            }
        }

        return result;
    }

    private Task CleanupOldBackupsAsync(string backupPath, int keepCount)
    {
        try
        {
            var files = Directory.GetFiles(backupPath, "backlog_db_backup_*.sql")
                .OrderByDescending(f => f)
                .Skip(keepCount)
                .ToList();

            foreach (var file in files)
            {
                File.Delete(file);
                _logger.LogInformation("Deleted old backup: {File}", file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not cleanup old backups.");
        }

        return Task.CompletedTask;
    }
}
