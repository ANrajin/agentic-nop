namespace NopCommerceAgents.Models;

public class SyncResult
{
    public List<string> Copied { get; set; } = [];
    public List<string> Skipped { get; set; } = [];
    public List<string> DirectoriesCreated { get; set; } = [];
}
