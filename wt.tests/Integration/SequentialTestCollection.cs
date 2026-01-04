namespace Kuju63.WorkTree.Tests.Integration;

/// <summary>
/// Sequential test collection to prevent parallel execution of integration tests.
/// This avoids resource conflicts and temporary directory cleanup timing issues.
/// </summary>
[CollectionDefinition("Sequential Integration Tests", DisableParallelization = true)]
public class SequentialTestCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and the collection name.
}
