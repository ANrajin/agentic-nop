using System.CommandLine;
using System.Text.Json;
using NopCommerceAgents.Models;
using NopCommerceAgents.Services;

namespace NopCommerceAgents.Commands;

public class InitCommand : Command
{
    public InitCommand() : base("init", "Clone repo, copy rules/skills, and generate AGENTS.md")
    {
        var branchOption = new Option<string>(
            name: "--branch",
            description: "The branch to clone from",
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

        var gitService = new GitService();
        var fileSyncService = new FileSyncService();
        var markdownGenerator = new AgentsMarkdownGenerator();

        try
        {
            // 1. Clone or Pull
            await gitService.CloneOrPullAsync(branch, cacheDir);
            var commitSha = await gitService.GetCurrentCommitShaAsync(cacheDir);

            // 2. Setup local agents dir
            if (!Directory.Exists(agentsDir))
            {
                Directory.CreateDirectory(agentsDir);
            }

            // 3. Copy files
            ConsoleOutput.Step("📁 Copying rules...");
            var rulesResult = await fileSyncService.SyncDirectoryAsync(
                Path.Combine(cacheDir, "src", "rules"), 
                Path.Combine(agentsDir, "rules"));
            PrintSyncResult(rulesResult);

            ConsoleOutput.Step("📁 Copying skills...");
            var skillsResult = await fileSyncService.SyncDirectoryAsync(
                Path.Combine(cacheDir, "src", "skills"), 
                Path.Combine(agentsDir, "skills"));
            PrintSyncResult(skillsResult);

            // 4. Generate AGENTS.md
            ConsoleOutput.Step("📝 Generating AGENTS.md...");
            await markdownGenerator.GenerateAsync(
                agentsDir, 
                Path.Combine(currentDir, Constants.AgentsMarkdownFile));

            // 5. Write source metadata
            var metadata = new SourceMetadata
            {
                CommitSha = commitSha,
                Branch = branch,
                SyncedAtUtc = DateTime.UtcNow
            };
            
            var metadataPath = Path.Combine(agentsDir, Constants.SourceMetadataFile);
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

            var totalCopied = rulesResult.Copied.Count + skillsResult.Copied.Count;
            ConsoleOutput.Step($"✅ Initialized {Constants.AgentsDirectory}/ with {totalCopied} files (commit {commitSha})");
        }
        catch (Exception ex)
        {
            ConsoleOutput.Error($"Initialization failed: {ex.Message}");
        }
    }

    private void PrintSyncResult(SyncResult result)
    {
        foreach (var file in result.Copied)
        {
            ConsoleOutput.Success(file);
        }
        
        foreach (var file in result.Skipped)
        {
            ConsoleOutput.Warning($"{file} (skipped — exists locally)");
        }

        if (result.Copied.Count == 0 && result.Skipped.Count == 0)
        {
            ConsoleOutput.Info("No files found.");
        }
    }
}
