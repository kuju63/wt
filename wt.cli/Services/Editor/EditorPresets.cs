using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Services.Editor;

/// <summary>
/// Provides preset configurations for known code editors.
/// </summary>
public static class EditorPresets
{
    /// <summary>
    /// Gets a dictionary of known editor configurations mapped by editor type.
    /// </summary>
    public static readonly Dictionary<EditorType, EditorConfig> KnownEditors = new()
    {
        [EditorType.VSCode] = new EditorConfig
        {
            EditorType = EditorType.VSCode,
            Command = "code",
            Arguments = "{path}"
        },
        [EditorType.Vim] = new EditorConfig
        {
            EditorType = EditorType.Vim,
            Command = "vim",
            Arguments = "{path}"
        },
        [EditorType.Emacs] = new EditorConfig
        {
            EditorType = EditorType.Emacs,
            Command = "emacs",
            Arguments = "{path}"
        },
        [EditorType.Nano] = new EditorConfig
        {
            EditorType = EditorType.Nano,
            Command = "nano",
            Arguments = "{path}"
        },
        [EditorType.IntelliJIDEA] = new EditorConfig
        {
            EditorType = EditorType.IntelliJIDEA,
            Command = "idea",
            Arguments = "{path}"
        }
    };
}
