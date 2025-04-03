using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NexodusAPI.Models;

public class Chat
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    public List<Message> Messages { get; set; } = new List<Message>();

    public DateTime CreatedAt { get; set; }

    public Chat()
    {
        CreatedAt = DateTime.UtcNow;
    }
}