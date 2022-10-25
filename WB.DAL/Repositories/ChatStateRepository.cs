using Microsoft.EntityFrameworkCore;
using WB.DAL.Exceptions;
using WB.DAL.Models;

namespace WB.DAL.Repositories;

public class ChatStateRepository
{
    public ChatStateRepository(WbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public WbContext Context { get; }
    
    public async Task<ChatState> GetAsync(int id)
    {
        var chatState = await Context.ChatStates.FirstOrDefaultAsync(x => x.UserChatId == id);

        if (chatState == null)
        {
            throw new NoValuesFoundException();
        }

        return chatState;
    }
    
    public async Task Insert(ChatState chatState)
    {
        Context.ChatStates.Add(chatState);

        await Context.SaveChangesAsync();
    }
    
    public async Task Update(ChatState chatState)
    {
        if (chatState == null)
            throw new ArgumentNullException(nameof(chatState));

        Context.Entry(chatState).State = EntityState.Modified;
        await Context.SaveChangesAsync();
        Context.Entry(chatState).State = EntityState.Detached;
    }
}