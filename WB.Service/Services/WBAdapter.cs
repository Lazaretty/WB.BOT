using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WB.Service.Models;

namespace WB.Service.Services;

public class WBAdapter
{
    private readonly HttpClient _httpClient;

    private readonly List<string> _proxies;

    private static readonly Dictionary<string, DateTime> _usedProxies = new();
    
    private readonly ILogger<WBAdapter> _logger;

    public WBAdapter(ILogger<WBAdapter> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        
        _httpClient.BaseAddress = new Uri(@"https://suppliers-stats.wildberries.ru/");
       
        var proxyService = new ProxyParser();

        _proxies = proxyService.GetProxyList().GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<Sale>?> GetSales(string apiToken, DateTimeOffset lastUpdate)
    {
        lastUpdate = lastUpdate.AddHours(3);

       // lastUpdate = DateTimeOffset.Now.AddHours(-4);
       
        Task<string> result = Task.FromResult(string.Empty);

        for (int i = 0; i < _proxies.Count / 10; i += 10)
        {
            var tasks = _proxies
                .Where(x =>
                {
                    if (!_usedProxies.TryGetValue(x, out var time)) return true;
                    if (time - DateTime.Now <= TimeSpan.FromMinutes(1)) return false;
                    _usedProxies.Remove(x);
                    return true;

                })
                .Skip(i)
                .Take(10)
                .Select(x => GetResponseByProxy(x, apiToken, lastUpdate));

            result = await Task.WhenAny(tasks);
        }

        var stringResult = string.Empty;

        if (result.IsFaulted)
        {
            lastUpdate = lastUpdate.AddHours(3);
            var response = await _httpClient.GetAsync(
                $"api/v1/supplier/sales?key={apiToken}&datefrom={lastUpdate.Year}-{lastUpdate.Month}-{lastUpdate.Day}T{lastUpdate.Hour}:{lastUpdate.Minute}:{lastUpdate.Second}Z&flag=0");

            stringResult = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ArraySegment<Sale>.Empty;
            }
        }

        stringResult = await result;

        return JsonConvert.DeserializeObject<IEnumerable<Sale>>(stringResult);
    }


    private async Task<string> GetResponseByProxy(string proxy, string apiToken, DateTimeOffset lastUpdate)
    {
        await Task.Delay(1);
        
        try
        {
            HttpWebRequest myReq = (HttpWebRequest)WebRequest
                .Create(
                    $"https://suppliers-stats.wildberries.ru/api/v1/supplier/sales?key={apiToken}&datefrom={lastUpdate.Year}-{lastUpdate.Month}-{lastUpdate.Day - 1}T{lastUpdate.Hour}:{lastUpdate.Minute}:{lastUpdate.Second}Z&flag=0");

            myReq.Method = WebRequestMethods.Http.Get;
            var proxyURI = new Uri("http://" + proxy);
            myReq.Proxy = new WebProxy(proxyURI);
            myReq.Timeout = 10_000;
            HttpWebResponse myResp = (HttpWebResponse)myReq.GetResponse();

            var stringResult = await new StreamReader(myResp.GetResponseStream()).ReadToEndAsync();
            
            _usedProxies.Add(proxy, DateTime.Now);
            
            return stringResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting updates from WB");
            return await Task.FromException<string>(ex);
        }
    }
}      