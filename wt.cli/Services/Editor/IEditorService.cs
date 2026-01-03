using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Services.Editor;

public interface IEditorService
{
    Task<CommandResult<string>> LaunchEditorAsync(string path, EditorType editorType, CancellationToken cancellationToken = default);
    EditorConfig ResolveEditorCommand(EditorType editorType);
}
