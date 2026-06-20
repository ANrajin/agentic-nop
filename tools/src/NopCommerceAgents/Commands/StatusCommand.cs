using System.CommandLine;
using System.Text.Json;
using NopCommerceAgents.Models;
using NopCommerceAgents.Services;

namespace NopCommerceAgents.Commands;

public class StatusCommand : Command
{
    public StatusCommand() : base("status", "Compare local vs cached repo")
    {
        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cacheDir = Path.Combine(userProfile, Constants.CacheDirectory, "cache");
        var currentDir = Directory.GetCurrentDirectory();
        var agentsDir = Path.Combine(currentDir, Constants.AgentsDirectory);
        var metadataPath = Path.Combine(agentsDir, Constants.SourceMetadataFile);

        if (!File.Exists(metadataPath))
        {
            ConsoleOutput.Error($"No {Constants.SourceMetadataFile} found. Please run 'nopcommerce-agents init' first.");
            return;
        }

        var gitService = new GitService();
        var fileSyncService = new FileSyncService();

        try
        {
            var metadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<SourceMetadata>(metadataJson);
            var syncedCommit = metadata?.CommitSha ?? "unknown";

            // Ensure cache is available, optionally pull to get latest target
            await gitService.CloneOrPullAsync(metadata?.Branch ?? Constants.DefaultBranch, cacheDir);
            var latestCommit = await gitService.GetCurrentCommitShaAsync(cacheDir);

            ConsoleOutput.Header($"📊 Status Report (synced: {syncedCommit} → latest: {latestCommit})");

            ConsoleOutput.Step("Rules:");
            var rulesDiff = await fileSyncService.CompareDirectoriesAsync(
                Path.Combine(cacheDir, "src", "rules"),
                Path.Combine(agentsDir, "rules")
            );
            PrintDiff(rulesDiff);

            ConsoleOutput.Step("Skills:");
            var skillsDiff = await fileSyncService.CompareDirectoriesAsync(
                Path.Combine(cacheDir, "src", "skills"),
                Path.Combine(agentsDir, "skills")
            );
            PrintDiff(skillsDiff);
            
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            ConsoleOutput.Error($"Status check failed: {ex.Message}");
        }
    }

    private void PrintDiff(List<FileComparisonResult> diff)
    {
        if (diff.Count == 0)
        {
            ConsoleOutput.Info("  No files to compare.");
            return;
        }

        foreach (var item in diff.OrderBy(x => x.RelativePath))
        {
            switch (item.Status)
            {
                case FileStatus.UpToDate:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✅ {item.RelativePath,-30} — Up to date");
                    break;
                case FileStatus.ModifiedLocally:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⚠️  {item.RelativePath,-30} — Modified locally");
                    break;
                case FileStatus.NewInRepo:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  🆕 {item.RelativePath,-30} — New in repo (run update)");
                    break;
                case FileStatus.MissingLocally:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ❌ {item.RelativePath,-30} — Missing locally");
                    break;
            }
            Console.ResetColor();
        }
    }
}
