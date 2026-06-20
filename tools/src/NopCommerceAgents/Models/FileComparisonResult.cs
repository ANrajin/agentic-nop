namespace NopCommerceAgents.Models;

public enum FileStatus 
{ 
    UpToDate, 
    ModifiedLocally, 
    MissingLocally, 
    NewInRepo 
}

public class FileComparisonResult
{
    public string RelativePath { get; set; } = string.Empty;
    public FileStatus Status { get; set; }
}
