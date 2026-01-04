namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents the configuration for a code editor.
/// </summary>
public class EditorConfig
{
    /// <summary>
    /// Gets or sets the type of the editor.
    /// </summary>
    public EditorType EditorType { get; set; }

    /// <summary>
    /// Gets or sets the command used to launch the editor.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command-line arguments template for the editor.
    /// </summary>
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the editor is available on the system.
    /// </summary>
    public bool IsAvailable { get; set; }
}
