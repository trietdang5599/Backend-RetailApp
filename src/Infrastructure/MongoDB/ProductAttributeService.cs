using MongoDB.Bson;
using MongoDB.Driver;
using ProductManagement.Application.Interfaces;
using System.Text.Json;

namespace ProductManagement.Infrastructure.MongoDB;

public class ProductAttributeService : IProductAttributeService
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public ProductAttributeService(IMongoClient mongoClient, string databaseName)
    {
        var db = mongoClient.GetDatabase(databaseName);
        _collection = db.GetCollection<BsonDocument>("product_attributes");
    }

    public async Task<string?> GetAttributesAsync(string documentId, CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(documentId));
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);
        if (doc is null) return null;
        doc.Remove("_id");
        doc.Remove("productId");
        return doc.ToJson();
    }

    public async Task<string> SaveAttributesAsync(Guid productId, Dictionary<string, object> attributes, CancellationToken ct = default)
    {
        var doc = new BsonDocument
        {
            ["productId"] = productId.ToString(),
            ["createdAt"] = DateTime.UtcNow
        };

        foreach (var (key, value) in attributes)
            doc[key] = BsonValue.Create(value?.ToString());

        await _collection.InsertOneAsync(doc, cancellationToken: ct);
        return doc["_id"].AsObjectId.ToString();
    }

    public async Task UpdateAttributesAsync(string documentId, Dictionary<string, object> attributes, CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(documentId));
        var updateDefs = new List<UpdateDefinition<BsonDocument>>
        {
            Builders<BsonDocument>.Update.Set("updatedAt", DateTime.UtcNow)
        };

        foreach (var (key, value) in attributes)
            updateDefs.Add(Builders<BsonDocument>.Update.Set(key, value?.ToString()));

        var update = Builders<BsonDocument>.Update.Combine(updateDefs);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task DeleteAttributesAsync(string documentId, CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(documentId));
        await _collection.DeleteOneAsync(filter, ct);
    }
}
