using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Types;

namespace WB.Telegram.API.Controllers;

//[Route("api")]
public class WebhookController : Controller
{
    private HandleUpdateService handleUpdateService;
    
    public WebhookController(HandleUpdateService handleUpdateService)
    {
        this.handleUpdateService = handleUpdateService;
    }
    
    //[HttpPost("update")]
    //public async Task<IActionResult> Post([FromBody] Update update)
    public async Task<IActionResult> Post()
    {
        
        Update update;
        using (var stream = new StreamReader(Request.Body))
        {
            var body = await stream.ReadToEndAsync();
            update =  JsonConvert.DeserializeObject<Update>(body); 
        } 
        
        await handleUpdateService.EchoAsync(update);
        return Ok();
    }
}