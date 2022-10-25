using Microsoft.EntityFrameworkCore;
using WB.DAL.Exceptions;
using WB.DAL.Models;

namespace WB.DAL.Repositories;

public class UserRepository
{
    public UserRepository(WbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public WbContext Context { get; }
    
    public async Task<User> GetAsync(int id)
    {
        var user = await Context.Users.FirstOrDefaultAsync(x => x.UserChatId == id);

        if (user == null)
        {
            throw new NoValuesFoundException();
        }

        return user;
    }

    public async Task<List<User>> GetAllActiveAsync()
    {
        var users = await Context.Users.Where(x => x.IsActive).ToListAsync();
        return users;
    }
    
    public async Task Insert(User user)
    {
        Context.Users.Add(user);

        await Context.SaveChangesAsync();
    }
    
    public Task<bool> IsUserExists(long chatId)
    {
        return Context.Users.AnyAsync(x => x.UserChatId == chatId);
    }
}