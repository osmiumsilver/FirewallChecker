namespace FirewallChecker;

public class FirewallRule
{
    public string Name { get; set; }
    public string Direction { get; set; }
    public string Action { get; set; }
    public string ApplicationPath { get; set; }
}