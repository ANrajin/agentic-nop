using System.Security.Cryptography;
using NopCommerceAgents.Models;

namespace NopCommerceAgents.Services;

public class FileSyncService
{
    public async Task<SyncResult> SyncDirectoryAsync(string sourceDir, string targetDir)
    {
        var result = new SyncResult();

        if (!Directory.Exists(sourceDir))
            return result;

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
            result.DirectoriesCreated.Add(targetDir);
        }

        var sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            // Skip .git directory and source metadata file
            if (sourceFile.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) ||
                Path.GetFileName(sourceFile) == Constants.SourceMetadataFile)
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var targetFile = Path.Combine(targetDir, relativePath);

            if (File.Exists(targetFile))
            {
                // We only copy if it doesn't exist to protect local changes. 
                // So if it exists, skip it. (Unless it's the update command handling changed files, 
                // but the plan says "Skips existing files with a warning").
                result.Skipped.Add(relativePath);
                continue;
            }

            var targetDirForFile = Path.GetDirectoryName(targetFile);
            if (targetDirForFile != null && !Directory.Exists(targetDirForFile))
            {
                Directory.CreateDirectory(targetDirForFile);
                result.DirectoriesCreated.Add(targetDirForFile);
            }

            File.Copy(sourceFile, targetFile);
            result.Copied.Add(relativePath);
        }

        return result;
    }

    public async Task<List<FileComparisonResult>> CompareDirectoriesAsync(string sourceDir, string targetDir)
    {
        var results = new List<FileComparisonResult>();

        if (!Directory.Exists(sourceDir))
            return results;

        var sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) &&
                        Path.GetFileName(f) != Constants.SourceMetadataFile);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var targetFile = Path.Combine(targetDir, relativePath);

            if (!File.Exists(targetFile))
            {
                results.Add(new FileComparisonResult { RelativePath = relativePath, Status = FileStatus.NewInRepo });
            }
            else
            {
                var sourceHash = await ComputeFileHashAsync(sourceFile);
                var targetHash = await ComputeFileHashAsync(targetFile);

                if (sourceHash == targetHash)
                {
                    results.Add(new FileComparisonResult { RelativePath = relativePath, Status = FileStatus.UpToDate });
                }
                else
                {
                    results.Add(new FileComparisonResult { RelativePath = relativePath, Status = FileStatus.ModifiedLocally });
                }
            }
        }

        // Check for files that exist locally but not in repo
        if (Directory.Exists(targetDir))
        {
            var targetFiles = Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f) != Constants.SourceMetadataFile);

            foreach (var targetFile in targetFiles)
            {
                var relativePath = Path.GetRelativePath(targetDir, targetFile);
                var sourceFile = Path.Combine(sourceDir, relativePath);

                if (!File.Exists(sourceFile))
                {
                    // This file is in the target dir but not in the source repo. 
                    // Technically it might just be a local user file.
                    // The plan doesn't mention "MissingLocally", but we can report it if we want.
                }
            }
        }

        return results;
    }

    private async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
