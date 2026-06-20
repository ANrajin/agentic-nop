namespace NopCommerceAgents.Models;

public class SourceMetadata
{
    public string CommitSha { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public DateTime SyncedAtUtc { get; set; }
}
