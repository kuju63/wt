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
        var outputOption = new Option<string>("--output")
        {
            Description = "Output directory for generated documentation",
            DefaultValueFactory = _ => "docs/commands"
        };

        var wtPathOption = new Option<string>("--wt-path")
        {
            Description = "Path to wt executable",
            DefaultValueFactory = _ => "wt.cli/bin/Release/net10.0/osx-arm64/wt"
        };

        var rootCommand = new RootCommand("Generate command documentation from System.CommandLine definitions");
        rootCommand.Options.Add(outputOption);
        rootCommand.Options.Add(wtPathOption);

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var output = parseResult.GetValue(outputOption) ?? "docs/commands";
            var wtPath = parseResult.GetValue(wtPathOption) ?? "wt.cli/bin/Release/net10.0/osx-arm64/wt";
            await GenerateDocumentation(output, wtPath);
            return 0;
        });

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    static async Task GenerateDocumentation(string outputPath, string wtPath)
    {
        Console.WriteLine($"Generating command documentation to: {outputPath}");
        Console.WriteLine($"Using wt executable: {wtPath}");
        
        // Create output directory if it doesn't exist
        Directory.CreateDirectory(outputPath);
        
        // Get the list of commands from wt --help
        var commands = await GetCommandsAsync(wtPath);
        
        // Generate documentation for each command
        foreach (var command in commands)
        {
            Console.WriteLine($"  Generating documentation for: {command}");
            var helpText = await GetCommandHelpAsync(wtPath, command);
            var markdown = CommandDocGenerator.ConvertHelpToMarkdown(command, helpText);
            var filePath = Path.Combine(outputPath, $"{command}.md");
            await File.WriteAllTextAsync(filePath, markdown);
            Console.WriteLine($"    Generated: {filePath}");
        }
        
        Console.WriteLine("Command documentation generation complete.");
    }

    static async Task<List<string>> GetCommandsAsync(string wtPath)
    {
        var result = await RunProcessAsync(wtPath, "--help");
        var commands = new List<string>();
        
        var lines = result.Split('\n');
        bool inCommandsSection = false;
        
        foreach (var line in lines)
        {
            if (line.Contains("コマンド:") || line.Contains("Commands:"))
            {
                inCommandsSection = true;
                continue;
            }
            
            if (inCommandsSection && !string.IsNullOrWhiteSpace(line))
            {
                // Extract command name from lines like "  create <branch>  Create a new worktree..."
                var trimmed = line.Trim();
                if (trimmed.StartsWith("-") || trimmed.StartsWith("--"))
                {
                    // This is an option, not a command
                    break;
                }
                
                var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    commands.Add(parts[0]);
                }
            }
        }
        
        return commands;
    }

    static async Task<string> GetCommandHelpAsync(string wtPath, string command)
    {
        return await RunProcessAsync(wtPath, $"{command} --help");
    }

    static async Task<string> RunProcessAsync(string command, string arguments)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return output;
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
    public static string ConvertHelpToMarkdown(string commandName, string helpText)
    {
        var console = new MarkdownConsole();
        var lines = helpText.Split('\n');
        
        // Parse help text sections
        string? description = null;
        string? usage = null;
        var arguments = new List<(string name, string desc)>();
        var options = new List<(string name, string desc)>();
        
        string? currentSection = null;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("Description:"))
            {
                currentSection = "description";
                continue;
            }
            else if (trimmed.Contains("使用法:") || trimmed.Contains("Usage:"))
            {
                currentSection = "usage";
                continue;
            }
            else if (trimmed.Contains("引数:") || trimmed.Contains("Arguments:"))
            {
                currentSection = "arguments";
                continue;
            }
            else if (trimmed.Contains("オプション:") || trimmed.Contains("Options:"))
            {
                currentSection = "options";
                continue;
            }
            
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }
            
            switch (currentSection)
            {
                case "description":
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        description = trimmed;
                    }
                    break;
                    
                case "usage":
                    if (trimmed.StartsWith("wt "))
                    {
                        usage = trimmed;
                    }
                    break;
                    
                case "arguments":
                    if (trimmed.StartsWith("<"))
                    {
                        var parts = trimmed.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            arguments.Add((parts[0], string.Join(" ", parts.Skip(1))));
                        }
                    }
                    break;
                    
                case "options":
                    if (trimmed.StartsWith("-"))
                    {
                        var parts = trimmed.Split(new[] { "  " }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            options.Add((parts[0], parts[1]));
                        }
                        else if (parts.Length == 1)
                        {
                            options.Add((parts[0], ""));
                        }
                    }
                    break;
            }
        }
        
        // Generate markdown
        console.WriteHeading(1, $"wt {commandName}");
        
        if (!string.IsNullOrEmpty(description))
        {
            console.WriteText(description);
        }
        
        console.WriteHeading(2, "Syntax");
        if (!string.IsNullOrEmpty(usage))
        {
            console.WriteCodeBlock("bash", usage);
        }
        
        if (arguments.Count > 0)
        {
            console.WriteHeading(2, "Arguments");
            foreach (var (name, desc) in arguments)
            {
                console.WriteText($"**`{name}`**  \n{desc}");
            }
        }
        
        if (options.Count > 0)
        {
            console.WriteHeading(2, "Options");
            foreach (var (name, desc) in options)
            {
                // Skip help option
                if (name.Contains("--help"))
                {
                    continue;
                }
                console.WriteText($"`{name}`  \n{desc}");
            }
        }
        
        console.WriteHeading(2, "Examples");
        console.WriteText("See the command reference documentation for usage examples.");
        
        return console.ToString();
    }
}
