namespace WB.DAL.Models;

public class User
{
    public long UserId { get; set; }
    public long UserChatId { get; set; }

    public string ApiKey { get; set; }

    public bool IsActive { get; set; }

    public ChatState ChatState { get; set; }
}