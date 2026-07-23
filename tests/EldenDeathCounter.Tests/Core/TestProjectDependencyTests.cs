namespace EldenDeathCounter.Tests.Core;

public sealed class TestProjectDependencyTests
{
    [Fact]
    public void CoreTestProjectDoesNotReferenceWpfApplicationProject()
    {
        var projectRoot = FindProjectRoot();
        var testProject = File.ReadAllText(Path.Combine(
            projectRoot,
            "tests",
            "EldenDeathCounter.Tests",
            "EldenDeathCounter.Tests.csproj"));

        Assert.DoesNotContain("src\\EldenDeathCounter\\EldenDeathCounter.csproj", testProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("src/EldenDeathCounter/EldenDeathCounter.csproj", testProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("..\\..\\src\\EldenDeathCounter\\EldenDeathCounter.csproj", testProject, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("../../src/EldenDeathCounter/EldenDeathCounter.csproj", testProject, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EldenDeathCounter.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find repository root.");
    }
}
