using MongoDB.Driver;

namespace NexodusAPI.Models
{
    public class UserContext
    {
        private readonly IMongoDatabase _database;

        public UserContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    }
}
