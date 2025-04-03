namespace NexodusAPI.Models
{
    public class Message
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public Message(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
