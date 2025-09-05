using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LitedbMcpServer;

public interface IDatabaseManager
{
    Task<ILiteDatabase> GetDatabaseAsync(string databaseName);
    Task<IEnumerable<string>> GetDatabaseNamesAsync();
    Task<DatabaseStats> GetDatabaseStatsAsync(string databaseName);
    Task<IEnumerable<string>> GetCollectionNamesAsync(string databaseName);
}

public class DatabaseManager : IDatabaseManager, IDisposable
{
    private readonly ILogger<DatabaseManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, ILiteDatabase> _databases;
    private readonly Dictionary<string, string> _databasePaths;

    public DatabaseManager(ILogger<DatabaseManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _databases = new ConcurrentDictionary<string, ILiteDatabase>();
        _databasePaths = new Dictionary<string, string>();
        
        LoadDatabaseConfiguration();
    }

    private void LoadDatabaseConfiguration()
    {
        // Get database path from configuration (set from command-line args)
        var databasePath = _configuration.GetValue<string>("DatabasePath");
        
        if (string.IsNullOrEmpty(databasePath))
        {
            _logger.LogError("No database path provided. Please specify a database path in mcp.json args.");
            throw new InvalidOperationException("Database path must be specified as a command-line argument.");
        }
        
        _logger.LogInformation("Using database path: {Path}", databasePath);
        
        // Ensure the directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Created directory for database: {Directory}", directory);
        }
        
        // Extract database name from the file path
        var databaseName = Path.GetFileNameWithoutExtension(databasePath);
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = "main";
        }
        
        // Set only one database path
        _databasePaths[databaseName] = databasePath;
        
        _logger.LogInformation("Configured database: {Database} at {Path}", databaseName, databasePath);
    }

    public async Task<ILiteDatabase> GetDatabaseAsync(string databaseName)
    {
        return await Task.FromResult(_databases.GetOrAdd(databaseName, name =>
        {
            if (!_databasePaths.TryGetValue(name, out var path))
            {
                throw new ArgumentException($"Database '{name}' not found in configuration");
            }

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var db = new LiteDatabase(path);
                _logger.LogInformation("Connected to database '{Name}' at '{Path}'", name, path);
                return db;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database '{Name}' at '{Path}'", name, path);
                throw;
            }
        }));
    }

    public async Task<IEnumerable<string>> GetDatabaseNamesAsync()
    {
        return await Task.FromResult(_databasePaths.Keys.AsEnumerable());
    }

    public async Task<DatabaseStats> GetDatabaseStatsAsync(string databaseName)
    {
        var db = await GetDatabaseAsync(databaseName);
        var collections = db.GetCollectionNames().ToList();
        
        var stats = new DatabaseStats
        {
            DatabaseName = databaseName,
            DatabasePath = _databasePaths[databaseName],
            CollectionCount = collections.Count,
            Collections = new List<CollectionStats>()
        };

        foreach (var collectionName in collections)
        {
            var collection = db.GetCollection(collectionName);
            var collectionStats = new CollectionStats
            {
                Name = collectionName,
                DocumentCount = collection.Count()
            };
            
            stats.Collections.Add(collectionStats);
            stats.TotalDocuments += collectionStats.DocumentCount;
        }

        // Get file size if possible
        try
        {
            var fileInfo = new FileInfo(_databasePaths[databaseName]);
            if (fileInfo.Exists)
            {
                stats.FileSizeBytes = fileInfo.Length;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get file size for database '{Name}'", databaseName);
        }

        return stats;
    }

    public async Task<IEnumerable<string>> GetCollectionNamesAsync(string databaseName)
    {
        var db = await GetDatabaseAsync(databaseName);
        return await Task.FromResult(db.GetCollectionNames());
    }

    public void Dispose()
    {
        foreach (var db in _databases.Values)
        {
            try
            {
                db.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing database connection");
            }
        }
        _databases.Clear();
    }
}

public class DatabaseStats
{
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabasePath { get; set; } = string.Empty;
    public int CollectionCount { get; set; }
    public int TotalDocuments { get; set; }
    public long FileSizeBytes { get; set; }
    public List<CollectionStats> Collections { get; set; } = new();
}

public class CollectionStats
{
    public string Name { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
}