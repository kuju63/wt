namespace wt.cli.Models;

public class EditorConfig
{
    public EditorType EditorType { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
