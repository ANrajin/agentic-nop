using System.Text;
using System.Text.RegularExpressions;

namespace NopCommerceAgents.Services;

public class AgentsMarkdownGenerator
{
    public async Task GenerateAsync(string agentsDirectory, string outputFile)
    {
        if (File.Exists(outputFile))
        {
            ConsoleOutput.Info($"{outputFile} already exists. Skipping generation.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("# NopCommerce Agents Configuration");
        sb.AppendLine();
        sb.AppendLine("This project uses centralized agent rules and skills to assist AI agents.");
        sb.AppendLine();

        // Process Rules
        var rulesDir = Path.Combine(agentsDirectory, "rules");
        if (Directory.Exists(rulesDir))
        {
            sb.AppendLine("## Installed Rules");
            sb.AppendLine();
            var rules = Directory.GetFiles(rulesDir, "*.md", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(f => f)
                .ToList();

            foreach (var rule in rules)
            {
                sb.AppendLine($"- `{rule}`");
            }
            sb.AppendLine();
        }

        // Process Skills
        var skillsDir = Path.Combine(agentsDirectory, "skills");
        if (Directory.Exists(skillsDir))
        {
            sb.AppendLine("## Installed Skills");
            sb.AppendLine();
            var skillFiles = Directory.GetFiles(skillsDir, "SKILL.md", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .ToList();

            foreach (var skillFile in skillFiles)
            {
                var relativePath = Path.GetRelativePath(skillsDir, skillFile).Replace('\\', '/');
                var skillDirName = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');
                
                var (name, description) = await ParseFrontmatterAsync(skillFile);
                var displayName = !string.IsNullOrEmpty(name) ? name : skillDirName;
                
                sb.AppendLine($"- **{displayName}** (`{skillDirName}/SKILL.md`)");
                if (!string.IsNullOrEmpty(description))
                {
                    sb.AppendLine($"  - {description}");
                }
            }
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(outputFile, sb.ToString());
        ConsoleOutput.Success($"Generated {outputFile}");
    }

    private async Task<(string? Name, string? Description)> ParseFrontmatterAsync(string filePath)
    {
        string? name = null;
        string? description = null;

        var lines = await File.ReadAllLinesAsync(filePath);
        if (lines.Length == 0 || lines[0].Trim() != "---")
            return (name, description);

        bool inFrontmatter = true;
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line == "---")
            {
                inFrontmatter = false;
                break;
            }

            if (inFrontmatter)
            {
                var match = Regex.Match(line, @"^(name|description):\s*(.+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var key = match.Groups[1].Value.ToLower();
                    var value = match.Groups[2].Value.Trim().Trim('"', '\'');

                    if (key == "name") name = value;
                    else if (key == "description") description = value;
                }
            }
        }

        return (name, description);
    }
}
