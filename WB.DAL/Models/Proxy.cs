namespace WB.DAL.Models;

public class Proxy
{
    public long ProxyId { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    
    public int SuccessfulUses { get; set; }
    public DateTime LastUsed { get; set; }
    public bool Active { get; set; }
}