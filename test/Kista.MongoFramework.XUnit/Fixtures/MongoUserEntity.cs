using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Kista;

public class MongoUserEntity : IHaveOwner<string>
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [DataOwner]
    public string OwnerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    string IHaveOwner<string>.Owner => OwnerId;

    void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
}
