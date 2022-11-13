using Newtonsoft.Json;
using WB.Service.Models;

namespace WB.Service.Services;

public class WBAdapter
{
    private readonly HttpClient _httpClient; 
    
    
    public WBAdapter()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(@"https://suppliers-stats.wildberries.ru/");
    }

    public async Task<IEnumerable<Sale>?> GetSales(string apiToken, DateTimeOffset lastUpdate)
    {
        var response = await _httpClient.GetAsync($"api/v1/supplier/sales?key={apiToken}&datefrom={lastUpdate.Year}-{lastUpdate.Month}-{lastUpdate.Day-1}T{lastUpdate.Hour}:{lastUpdate.Minute}:{lastUpdate.Second}Z&flag=0");
        response.EnsureSuccessStatusCode();
        
        if (!response.IsSuccessStatusCode)
        {
            await Task.Delay(30_000);
            return Array.Empty<Sale>();
        }

        var stringResult = await response.Content.ReadAsStringAsync();
        
        return JsonConvert.DeserializeObject<IEnumerable<Sale>>(stringResult);
    }
}