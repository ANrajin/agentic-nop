using System.Diagnostics;

namespace NopCommerceAgents.Services;

public class GitService
{
    public async Task CloneOrPullAsync(string branch, string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            ConsoleOutput.Step("🔄 Cloning nopcommerce-agents repository...");
            await RunGitCommandAsync($"clone -b {branch} {Constants.RepoUrl} \"{targetDirectory}\"", Directory.GetCurrentDirectory());
            ConsoleOutput.Success($"Cached at {targetDirectory}");
        }
        else
        {
            ConsoleOutput.Step("🔄 Pulling latest changes...");
            await RunGitCommandAsync("fetch --all", targetDirectory);
            await RunGitCommandAsync($"checkout {branch}", targetDirectory);
            await RunGitCommandAsync("pull", targetDirectory);
        }
    }

    public async Task<string> GetCurrentCommitShaAsync(string repositoryDirectory)
    {
        return await RunGitCommandAsync("rev-parse --short HEAD", repositoryDirectory);
    }

    private async Task<string> RunGitCommandAsync(string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        try
        {
            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = (await outputTask).Trim();
            var error = (await errorTask).Trim();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Git command failed (Exit Code {process.ExitCode}): {error}");
            }

            return output;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            throw new Exception("Git is not installed or not added to PATH. Please install Git to use this tool.");
        }
    }
}
