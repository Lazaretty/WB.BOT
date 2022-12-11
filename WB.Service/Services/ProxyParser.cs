using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Serilog;
using Serilog.Core;
using WB.DAL.Models;
using WB.DAL.Repositories;

namespace WB.Service.Services;

public class ProxyParser
{
    private readonly HttpClient _client;

    private List<int> _ports;

    private readonly ProxyRepository _proxyRepository;

    public ProxyParser(ProxyRepository proxyRepository)
    {
        _proxyRepository = proxyRepository;
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
            var url = $"https://spys.one/proxy-port/{port}/";                                                                                
            var web = new HtmlWeb();                                                                                                         
            var doc = await web.LoadFromWebAsync(url);
            var pattern = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):\d{1,5}\b");    
            var matches = pattern.Matches(doc.ParsedText);   
            
            result.AddRange(matches.Select(x => x.ToString()));
        }

        return result;
    }

    public async Task<int> ReadAndSaveProxiesFromFile(Stream content)
    {
        var reader = new StreamReader(content);
        var text = await reader.ReadToEndAsync();
        
        var pattern = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?):\d{1,5}\b");    
        var matches = pattern.Matches(text);

        foreach (var match in matches.Select(x => x.ToString()))
        {
            await _proxyRepository.InsertAsync(new Proxy()
            {
                Host = match.Split(':')[0],
                Port = Int32.Parse(match.Split(':')[1]),
                Active = true,
                LastUsed = DateTime.Now,
                SuccessfulUses = 0
            });
        }

        return matches.Count;
    }
}