# LiteDB MCP Server

A C# MCP server for interacting with LiteDB NoSQL databases through the Model Context Protocol.

## Features

- **Database Statistics**: Get comprehensive statistics and information about databases
- **Collection Management**: List all collections in a database with metadata
- **Document Operations**: Find, insert, and delete documents with powerful filtering
- **Query Support**: Filter documents using BsonExpression syntax
- **Pagination**: Control result sets with skip and limit parameters
- **Single Database Focus**: Simplified design that works with a single LiteDB database
- **MCP Protocol Compliant**: Full support for MCP tools and resources

---

## Prerequisites

- **.NET 9.0**
  - Check that the project file (`LitedbMcpServer.csproj`) is compatible with your environment by running:
  ```
  dotnet run --project /path/to/LitedbMcpServer/LitedbMcpServer.csproj
  ```

---

## Installation

Clone this repository:
```
git clone https://github.com/vishwanath-uppala/litedb-mcp-server.git
cd litedb-mcp-server
```

## Configuration

For **Amazon Q CLI** users: Add the following to `mcp.json`:

```json
{
  "mcpServers": {
    "litedb-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/full/path/to/LitedbMcpServer/LitedbMcpServer.csproj",
        "--no-build",
        "--",
        "--database-path",
        "/full/path/to/database/file.db"
      ]
    }
  }
}
```

The `--database-path` argument is required and must point to a valid file path where the LiteDB database will be stored.

---

# Usage

## Tools Provided

After successful installation, the following tools will be available to MCP client applications.

### Overview

| name | description |
| --- | --- |
| ListDatabases | Get the current database |
| ListCollections | List all collections in a database |
| FindDocuments | Find documents in a collection with optional filtering |
| GetDatabaseStats | Get statistics and information about a database |
| GetCollectionStats | Get detailed statistics and metadata for a specific collection |
| InsertDocument | Insert a document into a collection |
| DeleteDocuments | Delete documents from a collection |

### Detailed Description

- **ListDatabases**
  - Get the current database
  - Input parameters: None
  - Returns a string with the current database name

- **ListCollections**
  - List all collections in a database
  - Input parameters:
    - `database` (string, required): Database name
  - Returns a JSON array of collection names and metadata

- **FindDocuments**
  - Find documents in a LiteDB collection with optional filtering
  - Input parameters:
    - `database` (string, required): Database name
    - `collection` (string, required): Collection name
    - `filter` (string, optional): BsonExpression filter (e.g., '$.age > 25')
    - `limit` (integer, optional): Maximum number of documents to return (default: 100)
    - `skip` (integer, optional): Number of documents to skip (default: 0)
  - Returns a JSON array of documents matching the filter criteria

- **GetDatabaseStats**
  - Get statistics and information about a database
  - Input parameters:
    - `database` (string, required): Database name
  - Returns a JSON object with database statistics including size, collections, and document counts

- **GetCollectionStats**
  - Get detailed statistics and metadata for a specific collection
  - Input parameters:
    - `database` (string, required): Database name
    - `collection` (string, required): Collection name
  - Returns a JSON object with comprehensive collection analysis including:
    - Document count and size estimates
    - Field-by-field analysis with type information
    - Statistical data (min/max/average for numbers, length stats for strings, date ranges)
    - Data quality metrics (null counts, missing fields, unique values)
    - Analysis timestamp

- **InsertDocument**
  - Insert a document into a collection
  - Input parameters:
    - `database` (string, required): Database name
    - `collection` (string, required): Collection name
    - `documentJson` (string, required): JSON document to insert
  - Returns a confirmation message with the inserted document ID

- **DeleteDocuments**
  - Delete documents from a collection
  - Input parameters:
    - `database` (string, required): Database name
    - `collection` (string, required): Collection name
    - `filter` (string, required): BsonExpression filter (e.g., '$.age > 25')
  - Returns a confirmation message with the number of documents deleted

---

## Troubleshooting

For easy troubleshooting:

- Install the MCP Inspector:
  ```
  npm install -g @modelcontextprotocol/inspector
  ```

- Start the inspector:
  ```
  npx @modelcontextprotocol/inspector dotnet run --project /path/to/LitedbMcpServer/LitedbMcpServer.csproj -- --database-path "/path/to/data/users.db"
  ```

Access the provided URL to troubleshoot server interactions.