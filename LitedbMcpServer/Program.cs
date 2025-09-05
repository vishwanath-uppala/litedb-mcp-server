using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LitedbMcpServer;

// Parse command line arguments
string? databasePath = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--database-path" && i + 1 < args.Length)
    {
        databasePath = args[i + 1];
        i++; // Skip the next argument as it's the path
    }
}

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Create the MCP Server with Standard I/O Transport and Tools from the current assembly
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Logging.AddConsole(consoleLogOptions => {
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Configure the application with the database path if provided via command line
if (!string.IsNullOrEmpty(databasePath))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        { "DatabasePath", databasePath }
    });
}
else
{
    Console.Error.WriteLine("Error: No database path provided. Please specify --database-path in mcp.json args.");
    return;
}

// Add the database manager service
builder.Services.AddSingleton<IDatabaseManager, DatabaseManager>();

var app = builder.Build();
await app.RunAsync();