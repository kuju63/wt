using wt.cli.Models;

namespace wt.cli.Services.Editor;

public interface IEditorService
{
    Task<CommandResult<string>> LaunchEditorAsync(string path, EditorType editorType, CancellationToken cancellationToken = default);
    EditorConfig ResolveEditorCommand(EditorType editorType);
}
