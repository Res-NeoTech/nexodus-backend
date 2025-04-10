namespace NexodusAPI.Models
{
    public class ChatDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public ChatDTO(string id, string title) 
        {
            this.Id = id;
            this.Title = title;
        }
    }
}
