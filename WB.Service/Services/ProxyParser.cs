using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WB.Service.Services;

public class ProxyParser
{
    private readonly HttpClient _client;

    private List<int> _ports;

    public ProxyParser()
    {
        _ports = new List<int>()
        {
            8080
        };
        _client = new();
        _client.BaseAddress = new Uri("https://spys.one/");
    }

    public async Task<List<string>> GetProxyList()
    {
        var result = new List<string>();

        foreach (var port in _ports)
        {
            var url = $"https://spys.one/proxy-port/{8080}/";                                                                                
            var web = new HtmlWeb();                                                                                                         
            var doc = await web.LoadFromWebAsync(url);                                                                                                         
                                                                                                                                  
            Regex pattern = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):\d{1,5}\b");    
            var matchatches = pattern.Matches(doc.ParsedText);   
            
            result.AddRange(matchatches.Select(x => x.ToString()));
        }

        return result;
    }
}