using MongoDB.Driver;

namespace NexodusAPI.Models
{
    public class ChatContext
    {
        private readonly IMongoDatabase _database;

        public ChatContext(string connectionString, string databaseName) 
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<Chat> Chats => _database.GetCollection<Chat>("chats");
    }
}
