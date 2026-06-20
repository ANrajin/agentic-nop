using System.CommandLine;
using System.Text.Json;
using NopCommerceAgents.Models;
using NopCommerceAgents.Services;

namespace NopCommerceAgents.Commands;

public class UpdateCommand : Command
{
    public UpdateCommand() : base("update", "Pull latest changes, sync new files")
    {
        var branchOption = new Option<string>(
            name: "--branch",
            description: "The branch to pull from",
            getDefaultValue: () => Constants.DefaultBranch);

        AddOption(branchOption);

        this.SetHandler(ExecuteAsync, branchOption);
    }

    private async Task ExecuteAsync(string branch)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cacheDir = Path.Combine(userProfile, Constants.CacheDirectory, "cache");
        var currentDir = Directory.GetCurrentDirectory();
        var agentsDir = Path.Combine(currentDir, Constants.AgentsDirectory);
        var metadataPath = Path.Combine(agentsDir, Constants.SourceMetadataFile);

        if (!File.Exists(metadataPath))
        {
            ConsoleOutput.Error($"No {Constants.SourceMetadataFile} found in {Constants.AgentsDirectory}. Please run 'nopcommerce-agents init' first.");
            return;
        }

        var gitService = new GitService();
        var fileSyncService = new FileSyncService();
        var markdownGenerator = new AgentsMarkdownGenerator();

        try
        {
            var oldMetadataJson = await File.ReadAllTextAsync(metadataPath);
            var metadata = JsonSerializer.Deserialize<SourceMetadata>(oldMetadataJson);
            var oldCommit = metadata?.CommitSha ?? "unknown";

            // 1. Pull latest
            await gitService.CloneOrPullAsync(branch, cacheDir);
            var newCommit = await gitService.GetCurrentCommitShaAsync(cacheDir);

            ConsoleOutput.Success($"Updated cache ({oldCommit} → {newCommit})");

            // 2. Sync files
            ConsoleOutput.Step("📁 Syncing rules...");
            var rulesResult = await fileSyncService.SyncDirectoryAsync(
                Path.Combine(cacheDir, "src", "rules"), 
                Path.Combine(agentsDir, "rules"));
            PrintSyncResult(rulesResult);

            ConsoleOutput.Step("📁 Syncing skills...");
            var skillsResult = await fileSyncService.SyncDirectoryAsync(
                Path.Combine(cacheDir, "src", "skills"), 
                Path.Combine(agentsDir, "skills"));
            PrintSyncResult(skillsResult);

            // 3. Regenerate AGENTS.md if it doesn't exist (generator skips if exists)
            await markdownGenerator.GenerateAsync(
                agentsDir, 
                Path.Combine(currentDir, Constants.AgentsMarkdownFile));

            // 4. Update metadata
            if (metadata != null)
            {
                metadata.CommitSha = newCommit;
                metadata.Branch = branch;
                metadata.SyncedAtUtc = DateTime.UtcNow;
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
            }

            var totalCopied = rulesResult.Copied.Count + skillsResult.Copied.Count;
            var totalSkipped = rulesResult.Skipped.Count + skillsResult.Skipped.Count;
            ConsoleOutput.Step($"✅ Updated {Constants.AgentsDirectory}/ — {totalCopied} new files, {totalSkipped} skipped");
        }
        catch (Exception ex)
        {
            ConsoleOutput.Error($"Update failed: {ex.Message}");
        }
    }

    private void PrintSyncResult(SyncResult result)
    {
        foreach (var file in result.Copied)
        {
            ConsoleOutput.Success($"{file} (new)");
        }
        
        foreach (var file in result.Skipped)
        {
            ConsoleOutput.Warning($"{file} (skipped — exists locally)");
        }

        if (result.Copied.Count == 0 && result.Skipped.Count == 0)
        {
            ConsoleOutput.Info("No changes.");
        }
    }
}
