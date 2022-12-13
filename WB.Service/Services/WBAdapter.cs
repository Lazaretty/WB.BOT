using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WB.DAL.Models;
using WB.DAL.Repositories;
using WB.Service.Models;

namespace WB.Service.Services;

public class WBAdapter
{
    private readonly HttpClient _httpClient;

    private ConcurrentBag<Proxy> _proxies;
    private ConcurrentBag<Proxy> _usedProxies;
    private ConcurrentBag<Proxy> _successfulUsedProxies;
    private ConcurrentBag<Proxy> _inActiveProxy;

    private readonly ILogger<WBAdapter> _logger;

    private readonly ProxyRepository _proxyRepository;
    
    public WBAdapter(ILogger<WBAdapter> logger, ProxyRepository proxyRepository)
    {
        _logger = logger;
        _proxyRepository = proxyRepository;
        _httpClient = new HttpClient();
        _proxies = new ConcurrentBag<Proxy>();
        _usedProxies = new ConcurrentBag<Proxy>();
        _successfulUsedProxies = new ConcurrentBag<Proxy>();
        _inActiveProxy = new ConcurrentBag<Proxy>();
        _httpClient.BaseAddress = new Uri(@"https://suppliers-stats.wildberries.ru/");
    }

    public async Task Init()
    {
        _proxies.Clear();
        _usedProxies.Clear();
        _successfulUsedProxies.Clear();
        
        foreach (var freshestProxy in await _proxyRepository.GetFreshestProxies(100))
        {
            _proxies.Add(freshestProxy);
        }
    }

    public async Task<IEnumerable<Sale>?> GetSales(string apiToken, DateTimeOffset lastUpdate)
    {
        lastUpdate = lastUpdate.AddHours(3);

        //lastUpdate = DateTimeOffset.Now.AddHours(-4);

        string result = "-1";

        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < _proxies.Count; i += 20)
        {
            if(result != "-1") continue;
            
            var proxies = _proxies
                .Where(x => DateTime.Now - x.LastUsed > TimeSpan.FromMinutes(3))
                .Skip(i)
                .Take(20);

            foreach (var x in proxies) 
                tasks.Add(GetResponseByProxy(x, apiToken, lastUpdate));

            while (tasks.Any())
            {
                var taskCompleted = await Task.WhenAny(tasks);
                tasks.Remove(taskCompleted);
                result = await taskCompleted;
                if (result != "-1")
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }
        }

        foreach (var usedProxy in _usedProxies)
        {
            usedProxy.SuccessfulUses--;
            await _proxyRepository.Update(usedProxy);
        }
            
        foreach (var successfulUsedProxy in _successfulUsedProxies)
        {
            successfulUsedProxy.SuccessfulUses += 2;
            await _proxyRepository.Update(successfulUsedProxy);
        }
            
        foreach (var inactiveProxy in _inActiveProxy)
        {
            inactiveProxy.Active = false;
            await _proxyRepository.Update(inactiveProxy);
        }
        
        string stringResult;
        
        if (result == "-1" || _proxies.Count == 0)
        {
            var response = await _httpClient.GetAsync(
                $"api/v1/supplier/sales?key={apiToken}&datefrom={lastUpdate.Year}-{lastUpdate.Month}-{lastUpdate.Day}T{lastUpdate.Hour}:{lastUpdate.Minute}:{lastUpdate.Second}Z&flag=0");

            stringResult = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return ArraySegment<Sale>.Empty;
            }
        }
        else
        {
            stringResult = result;
        }

        return JsonConvert.DeserializeObject<IEnumerable<Sale>>(stringResult);
    }


    private async Task<string> GetResponseByProxy(Proxy proxy, string apiToken, DateTimeOffset lastUpdate)
    {
        await Task.Delay(1);

        try
        {
            HttpWebRequest myReq = (HttpWebRequest)WebRequest
                .Create(
                    $"https://suppliers-stats.wildberries.ru/api/v1/supplier/sales?key={apiToken}&datefrom={lastUpdate.Year}-{lastUpdate.Month}-{lastUpdate.Day}T{lastUpdate.Hour}:{lastUpdate.Minute}:{lastUpdate.Second}Z&flag=0");

            proxy.LastUsed = DateTime.Now;
            _usedProxies.Add(proxy);
            
            myReq.Method = WebRequestMethods.Http.Get;
            var proxyURI = new Uri("http://" + $"{proxy.Host}:{proxy.Port}");
            myReq.Proxy = new WebProxy(proxyURI);
            myReq.Timeout = 10_000;
            HttpWebResponse myResp = (HttpWebResponse)myReq.GetResponse();
            
            var stringResult = await new StreamReader(myResp.GetResponseStream()).ReadToEndAsync();

            _successfulUsedProxies.Add(proxy);
            
            return stringResult;
        } 
        catch (WebException ex)
        {
            if (ex.Message.Contains("No such host is known"))
            {
                _inActiveProxy.Add(proxy);
            }

            //_logger.LogError(ex, "Error while getting updates from WB");
            return "-1";
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Error while getting updates from WB");
            return "-1";
        }
    }
}      