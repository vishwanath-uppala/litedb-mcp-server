using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteDB;
using ModelContextProtocol.Server;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LitedbMcpServer;

[McpServerToolType]
public static class LiteDbTools
{
    [McpServerTool, Description("Get the current database")]
    public static async Task<string> ListDatabases(
        [Description("The database manager service")] IDatabaseManager databaseManager)
    {
        try
        {
            var databases = await databaseManager.GetDatabaseNamesAsync();
            var databaseName = databases.FirstOrDefault() ?? "unknown";
            var databasePath = "";
            
            try
            {
                var stats = await databaseManager.GetDatabaseStatsAsync(databaseName);
                databasePath = stats.DatabasePath;
            }
            catch { /* Ignore errors getting path */ }
            
            return $"Current database: {databaseName}\nPath: {databasePath}";
        }
        catch (Exception ex)
        {
            return $"Error getting database: {ex.Message}";
        }
    }
    
    [McpServerTool, Description("Find documents in a LiteDB collection with optional filtering")]
    public static async Task<string> FindDocuments(
        [Description("The database manager service")] IDatabaseManager databaseManager,
        [Description("Database name")] string database,
        [Description("Collection name")] string collection,
        [Description("Optional BsonExpression filter (e.g., '$.age > 25')")] string? filter = null,
        [Description("Maximum number of documents to return (default: 100)")] int limit = 100,
        [Description("Number of documents to skip (default: 0)")] int skip = 0)
    {
        try
        {
            var db = await databaseManager.GetDatabaseAsync(database);
            var col = db.GetCollection(collection);

            IEnumerable<BsonDocument> documents;

            if (!string.IsNullOrEmpty(filter))
            {
                documents = col.Find(BsonExpression.Create(filter), skip, limit);
            }
            else
            {
                documents = col.FindAll().Skip(skip).Take(limit);
            }

            var jsonDocuments = documents.Select(doc => JsonSerializer.Deserialize<JsonNode>(doc.ToString())).ToList();
            var resultText = JsonSerializer.Serialize(jsonDocuments, new JsonSerializerOptions { WriteIndented = true });

            return $"Found {jsonDocuments.Count} documents in {database}.{collection}:\n\n{resultText}";
        }
        catch (Exception ex)
        {
            return $"Error finding documents: {ex.Message}";
        }
    }
    
    [McpServerTool, Description("Get statistics and information about a database")]
    public static async Task<string> GetDatabaseStats(
        [Description("The database manager service")] IDatabaseManager databaseManager,
        [Description("Database name")] string database)
    {
        try
        {
            var stats = await databaseManager.GetDatabaseStatsAsync(database);
            var statsText = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });

            return $"Database statistics for '{database}':\n\n{statsText}";
        }
        catch (Exception ex)
        {
            return $"Error getting database stats: {ex.Message}";
        }
    }
    
    [McpServerTool, Description("List all collections in a database")]
    public static async Task<string> ListCollections(
        [Description("The database manager service")] IDatabaseManager databaseManager,
        [Description("Database name")] string database)
    {
        try
        {
            var collections = await databaseManager.GetCollectionNamesAsync(database);
            var collectionsList = collections.ToList();
            
            return $"Collections in database '{database}' ({collectionsList.Count}):\n\n" + 
                   string.Join("\n", collectionsList.Select(col => $"- {col}"));
        }
        catch (Exception ex)
        {
            return $"Error listing collections: {ex.Message}";
        }
    }
    
    [McpServerTool, Description("Insert a document into a collection")]
    public static async Task<string> InsertDocument(
        [Description("The database manager service")] IDatabaseManager databaseManager,
        [Description("Database name")] string database,
        [Description("Collection name")] string collection,
        [Description("JSON document to insert")] string documentJson)
    {
        try
        {
            var db = await databaseManager.GetDatabaseAsync(database);
            var col = db.GetCollection(collection);
            
            // Parse the JSON document using JsonDocument
            using var jsonDoc = JsonDocument.Parse(documentJson);
            
            // Convert JsonDocument to BsonDocument
            var bsonDoc = JsonElementToBsonDocument(jsonDoc.RootElement);
            
            // Insert the document
            var id = col.Insert(bsonDoc);
            
            return $"Document inserted successfully into {database}.{collection} with ID: {id}";
        }
        catch (Exception ex)
        {
            return $"Error inserting document: {ex.Message}";
        }
    }
    
    // Helper method to convert JsonElement to BsonDocument
    private static BsonDocument JsonElementToBsonDocument(JsonElement element)
    {
        var doc = new BsonDocument();
        
        foreach (var property in element.EnumerateObject())
        {
            doc[property.Name] = JsonElementToBsonValue(property.Value);
        }
        
        return doc;
    }
    
    // Helper method to convert JsonElement to BsonValue
    private static BsonValue JsonElementToBsonValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return BsonValue.Null;
                
            case JsonValueKind.True:
                return new BsonValue(true);
                
            case JsonValueKind.False:
                return new BsonValue(false);
                
            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                    return new BsonValue(intValue);
                if (element.TryGetInt64(out long longValue))
                    return new BsonValue(longValue);
                if (element.TryGetDouble(out double doubleValue))
                    return new BsonValue(doubleValue);
                return new BsonValue(element.GetDecimal());
                
            case JsonValueKind.String:
                var str = element.GetString();
                if (str != null && DateTime.TryParse(str, out DateTime dateValue))
                    return new BsonValue(dateValue);
                return new BsonValue(str ?? string.Empty);
                
            case JsonValueKind.Array:
                var array = new BsonArray();
                foreach (var item in element.EnumerateArray())
                    array.Add(JsonElementToBsonValue(item));
                return array;
                
            case JsonValueKind.Object:
                return JsonElementToBsonDocument(element);
                
            default:
                return BsonValue.Null;
        }
    }
    
    [McpServerTool, Description("Delete documents from a collection")]
    public static async Task<string> DeleteDocuments(
        [Description("The database manager service")] IDatabaseManager databaseManager,
        [Description("Database name")] string database,
        [Description("Collection name")] string collection,
        [Description("BsonExpression filter (e.g., '$.age > 25')")] string filter)
    {
        try
        {
            var db = await databaseManager.GetDatabaseAsync(database);
            var col = db.GetCollection(collection);
            
            // Delete documents matching the filter
            int count = col.DeleteMany(BsonExpression.Create(filter));
            
            return $"Deleted {count} documents from {database}.{collection}";
        }
        catch (Exception ex)
        {
            return $"Error deleting documents: {ex.Message}";
        }
    }
    
    [McpServerTool, Description("Get detailed statistics and metadata for a specific collection")]
    public static async Task<string> GetCollectionStats(
        [Description("The database manager service")] IDatabaseManager databaseManager,
        [Description("Database name")] string database,
        [Description("Collection name")] string collection)
    {
        try
        {
            var db = await databaseManager.GetDatabaseAsync(database);
            var col = db.GetCollection(collection);
            
            // Check if collection exists and has documents
            var documentCount = col.Count();
            if (documentCount == 0)
            {
                var emptyStats = new
                {
                    collectionName = collection,
                    documentCount = 0,
                    message = "Collection is empty or does not exist"
                };
                return JsonSerializer.Serialize(emptyStats, new JsonSerializerOptions { WriteIndented = true });
            }
            
            // Get sample documents for field analysis (limit to 1000 for performance)
            var sampleSize = Math.Min(1000, documentCount);
            var sampleDocs = col.FindAll().Take(sampleSize).ToList();
            
            // Calculate collection statistics
            var stats = new
            {
                collectionName = collection,
                documentCount = documentCount,
                sampleSize = sampleSize,
                totalSize = CalculateCollectionSize(sampleDocs, documentCount),
                averageDocumentSize = CalculateAverageDocumentSize(sampleDocs),
                indexes = GetIndexInformation(col),
                fieldAnalysis = AnalyzeFields(sampleDocs),
                analysisTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            
            var statsJson = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            return $"Collection statistics for '{database}.{collection}':\n\n{statsJson}";
        }
        catch (Exception ex)
        {
            return $"Error getting collection stats: {ex.Message}";
        }
    }
    
    // Helper method to calculate estimated collection size
    private static string CalculateCollectionSize(List<BsonDocument> sampleDocs, int totalDocuments)
    {
        if (sampleDocs.Count == 0) return "0 B";
        
        // Calculate average size of sample documents
        var totalSampleSize = sampleDocs.Sum(doc => doc.ToString().Length);
        var averageDocSize = totalSampleSize / (double)sampleDocs.Count;
        
        // Estimate total collection size
        var estimatedTotalSize = (long)(averageDocSize * totalDocuments);
        
        return FormatBytes(estimatedTotalSize);
    }
    
    // Helper method to calculate average document size
    private static string CalculateAverageDocumentSize(List<BsonDocument> sampleDocs)
    {
        if (sampleDocs.Count == 0) return "0 B";
        
        var totalSize = sampleDocs.Sum(doc => doc.ToString().Length);
        var averageSize = totalSize / sampleDocs.Count;
        
        return FormatBytes(averageSize);
    }
    
    // Helper method to format bytes into human-readable format
    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        double number = bytes;
        
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        
        return $"{number:n1} {suffixes[counter]}";
    }
    
    // Helper method to get index information
    private static List<object> GetIndexInformation(ILiteCollection<BsonDocument> collection)
    {
        var indexes = new List<object>();
        
        // LiteDB always has an _id index
        indexes.Add(new
        {
            field = "_id",
            type = "primary",
            unique = true
        });
        
        // Note: LiteDB doesn't provide easy access to custom index information
        // This would need to be enhanced based on your specific indexing strategy
        
        return indexes;
    }
    
    // Helper method to analyze fields in the collection
    private static Dictionary<string, object> AnalyzeFields(List<BsonDocument> documents)
    {
        var fieldStats = new Dictionary<string, object>();
        
        if (documents.Count == 0) return fieldStats;
        
        // Get all unique field names from sample documents
        var allFields = documents
            .SelectMany(d => d.Keys)
            .Distinct()
            .OrderBy(f => f)
            .ToList();
        
        foreach (var field in allFields)
        {
            var fieldValues = documents
                .Where(d => d.ContainsKey(field))
                .Select(d => d[field])
                .ToList();
                
            fieldStats[field] = AnalyzeField(fieldValues, documents.Count);
        }
        
        return fieldStats;
    }
    
    // Helper method to analyze a specific field
    private static object AnalyzeField(List<BsonValue> values, int totalDocuments)
    {
        var nonNullValues = values.Where(v => !v.IsNull).ToList();
        var nullCount = totalDocuments - values.Count; // Documents without this field
        var explicitNullCount = values.Count(v => v.IsNull); // Documents with explicit null
        
        if (nonNullValues.Count == 0)
        {
            return new
            {
                type = "null",
                presentInDocuments = values.Count,
                nullCount = explicitNullCount,
                missingCount = nullCount,
                uniqueCount = 0
            };
        }
        
        // Determine the most common type
        var typeGroups = nonNullValues.GroupBy(v => v.Type).ToList();
        var dominantType = typeGroups.OrderByDescending(g => g.Count()).First();
        
        var analysis = new Dictionary<string, object>
        {
            ["type"] = dominantType.Key.ToString().ToLower(),
            ["presentInDocuments"] = values.Count,
            ["nullCount"] = explicitNullCount,
            ["missingCount"] = nullCount,
            ["uniqueCount"] = nonNullValues.Distinct().Count()
        };
        
        // Add type-specific statistics
        if (dominantType.Key == BsonType.Int32 || dominantType.Key == BsonType.Int64 || dominantType.Key == BsonType.Double)
        {
            try
            {
                var numericValues = dominantType.Select(v => v.AsDouble).ToList();
                if (numericValues.Count > 0)
                {
                    analysis["min"] = numericValues.Min();
                    analysis["max"] = numericValues.Max();
                    analysis["average"] = Math.Round(numericValues.Average(), 2);
                }
            }
            catch
            {
                // Handle any conversion errors gracefully
            }
        }
        else if (dominantType.Key == BsonType.String)
        {
            try
            {
                var stringValues = dominantType.Select(v => v.AsString).Where(s => !string.IsNullOrEmpty(s)).ToList();
                if (stringValues.Count > 0)
                {
                    analysis["minLength"] = stringValues.Min(s => s.Length);
                    analysis["maxLength"] = stringValues.Max(s => s.Length);
                    analysis["averageLength"] = Math.Round(stringValues.Average(s => s.Length), 1);
                }
            }
            catch
            {
                // Handle any conversion errors gracefully
            }
        }
        else if (dominantType.Key == BsonType.DateTime)
        {
            try
            {
                var dateValues = dominantType.Select(v => v.AsDateTime).ToList();
                if (dateValues.Count > 0)
                {
                    analysis["earliest"] = dateValues.Min().ToString("yyyy-MM-ddTHH:mm:ssZ");
                    analysis["latest"] = dateValues.Max().ToString("yyyy-MM-ddTHH:mm:ssZ");
                }
            }
            catch
            {
                // Handle any conversion errors gracefully
            }
        }
        
        // Add type distribution if there are mixed types
        if (typeGroups.Count > 1)
        {
            analysis["typeDistribution"] = typeGroups.ToDictionary(
                g => g.Key.ToString().ToLower(),
                g => g.Count()
            );
        }
        
        return analysis;
    }
}