using Sofistike.Domain;

namespace Sofistike.UnitTests;

public sealed class DependencyRulesTests
{
    [Fact]
    public void DomainDoesNotReferenceOtherSolutionProjects()
    {
        var solutionReferences = typeof(AssemblyReference)
            .Assembly
            .GetReferencedAssemblies()
            .Where(reference =>
                reference.Name?.StartsWith("Sofistike.", StringComparison.Ordinal)
                is true
            );

        Assert.Empty(solutionReferences);
    }
}
