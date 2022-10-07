using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Types;

namespace WB.Telegram.API.Controllers;

public class WebhookController : Controller
{
    private HandleUpdateService handleUpdateService;
    
    public WebhookController(HandleUpdateService handleUpdateService)
    {
        this.handleUpdateService = handleUpdateService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        await handleUpdateService.EchoAsync(update);
        return Ok();
    }
}