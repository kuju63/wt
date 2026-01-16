using System.CommandLine;
using System.Text;

namespace DocGenerator;

/// <summary>
/// Command documentation generator for System.CommandLine
/// </summary>
static class Program
{
    static async Task<int> Main(string[] args)
    {
        var outputOption = new Option<string>(
            name: "--output",
            description: "Output directory for generated documentation",
            getDefaultValue: () => "docs/commands");

        var rootCommand = new RootCommand("Generate command documentation from System.CommandLine definitions")
        {
            outputOption
        };

        rootCommand.SetHandler(async (string output) =>
        {
            await GenerateDocumentation(output);
        }, outputOption);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task GenerateDocumentation(string outputPath)
    {
        Console.WriteLine($"Generating command documentation to: {outputPath}");
        
        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputPath);
        
        // TODO: Load command definitions from wt.cli assembly
        // For now, create placeholder structure
        Console.WriteLine("Command documentation generation complete.");
    }
}

/// <summary>
/// Markdown console for formatted output
/// </summary>
class MarkdownConsole
{
    private readonly StringBuilder _builder = new();

    public void WriteHeading(int level, string text)
    {
        _builder.AppendLine($"{new string('#', level)} {text}");
        _builder.AppendLine();
    }

    public void WriteText(string text)
    {
        _builder.AppendLine(text);
        _builder.AppendLine();
    }

    public void WriteCodeBlock(string language, string code)
    {
        _builder.AppendLine($"```{language}");
        _builder.AppendLine(code);
        _builder.AppendLine("```");
        _builder.AppendLine();
    }

    public override string ToString() => _builder.ToString();
}

/// <summary>
/// Command documentation generator
/// </summary>
static class CommandDocGenerator
{
    public static string ConvertCommandToMarkdown(Command command)
    {
        var console = new MarkdownConsole();
        
        // Command name and description
        console.WriteHeading(1, command.Name);
        console.WriteText(command.Description ?? "No description available.");
        
        // Syntax
        console.WriteHeading(2, "Syntax");
        console.WriteCodeBlock("bash", $"wt {command.Name} [options]");
        
        // Options
        if (command.Options.Any())
        {
            console.WriteHeading(2, "Options");
            foreach (var option in command.Options)
            {
                console.WriteText($"**{string.Join(", ", option.Aliases)}**: {option.Description}");
            }
        }
        
        // Arguments
        if (command.Arguments.Any())
        {
            console.WriteHeading(2, "Arguments");
            foreach (var argument in command.Arguments)
            {
                console.WriteText($"**{argument.Name}**: {argument.Description}");
            }
        }
        
        return console.ToString();
    }
}
