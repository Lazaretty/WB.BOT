
using WB.Common.Enums;

namespace WB.DAL.Models;

public class ChatState
{
    public long ChatStateId { get; set; }
    public long UserChatId { get; set; }

    public ChatSate State { get; set; }

    public User User { get; set; }
}