using wt.cli.Models;

namespace wt.cli.Services.Editor;

public static class EditorPresets
{
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
