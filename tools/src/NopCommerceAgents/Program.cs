using System.CommandLine;
using NopCommerceAgents.Commands;

namespace NopCommerceAgents;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Centralized Agent Rules & Skills Distribution Tool for nopCommerce");

        rootCommand.AddCommand(new InitCommand());
        rootCommand.AddCommand(new UpdateCommand());
        rootCommand.AddCommand(new StatusCommand());

        return await rootCommand.InvokeAsync(args);
    }
}
