using System.Reflection;
using DistributedChat.Application.Authentication;
using DistributedChat.Domain.Users;

namespace DistributedChat.UnitTests.Architecture;

public sealed class ProjectDependencyTests
{
    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrApi()
    {
        AssertAssemblyDoesNotReference(
            typeof(AuthService).Assembly,
            "DistributedChat.Infrastructure",
            "DistributedChat.Api");
    }

    [Fact]
    public void DomainDoesNotReferenceApplicationInfrastructureOrApi()
    {
        AssertAssemblyDoesNotReference(
            typeof(User).Assembly,
            "DistributedChat.Application",
            "DistributedChat.Infrastructure",
            "DistributedChat.Api");
    }

    private static void AssertAssemblyDoesNotReference(Assembly assembly, params string[] forbiddenAssemblyNames)
    {
        var forbiddenReferences = assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .Where(name => name is not null && forbiddenAssemblyNames.Contains(name, StringComparer.Ordinal))
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }
}
