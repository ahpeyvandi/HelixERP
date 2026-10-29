using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Platform;

namespace HelixERP.Architecture.Tests;

public sealed class NamespaceConventionTests
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        "bin",
        "obj",
        "dist",
        "coverage",
        "TestResults",
        ".git",
        ".vs",
        "volumes"
    };

    [Fact]
    public void Every_source_type_lives_in_a_HelixERP_namespace()
    {
        var violations = new List<string>();

        foreach (var assemblyName in SourceAssemblies.Names)
        {
            var assembly = SourceAssemblies.Load(assemblyName);

            foreach (var type in SafeGetTypes(assembly))
            {
                if (type.Name == "Program" || type.Name.StartsWith("<", StringComparison.Ordinal))
                {
                    continue;
                }

                if (type.FullName is null || !type.FullName.StartsWith("HelixERP.", StringComparison.Ordinal))
                {
                    violations.Add($"{assemblyName}: {type.FullName ?? type.Name}");
                }
            }
        }

        violations.Should().BeEmpty("all production types must use the HelixERP namespace prefix");
    }

    [Fact]
    public void Assemblies_are_named_with_the_HelixERP_prefix()
    {
        SourceAssemblies.Names.Should().OnlyContain(name => name.StartsWith("HelixERP.", StringComparison.Ordinal));
    }

    [Fact]
    public void Module_registration_lives_in_building_blocks_domain()
    {
        typeof(ModuleRegistration).Assembly.GetName().Name.Should().Be("HelixERP.BuildingBlocks.Domain");
    }

    [Fact]
    public void Source_code_never_mentions_nginx()
    {
        var sourceRoot = Path.Combine(RepositoryRoot(), "src");
        var offenders = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(file => !IsInExcludedDirectory(file))
            .Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                           file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                           file.EndsWith(".ts", StringComparison.OrdinalIgnoreCase) ||
                           file.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
            .Where(file => File.ReadAllText(file).Contains("nginx", StringComparison.OrdinalIgnoreCase))
            .ToList();

        offenders.Should().BeEmpty("AD001 forbids Nginx; YARP inside Shell is the gateway");
    }

    [Fact]
    public void Nginx_scan_ignores_build_and_vendor_directories()
    {
        var sourceRoot = Path.Combine(RepositoryRoot(), "src");

        IsInExcludedDirectory(Path.Combine(sourceRoot, "Frontend", "helix-shell", "node_modules", "mime-db", "db.json"))
            .Should().BeTrue("third-party packages must not be treated as project source");

        IsInExcludedDirectory(Path.Combine(sourceRoot, "Shell", "HelixERP.Shell.Api", "bin", "Debug", "appsettings.json"))
            .Should().BeTrue("build output must not be treated as project source");

        IsInExcludedDirectory(Path.Combine(sourceRoot, "Shell", "HelixERP.Shell.Api", "Program.cs"))
            .Should().BeFalse("first-party source files must still be scanned");
    }

    private static bool IsInExcludedDirectory(string file)
    {
        var directory = Path.GetDirectoryName(file);

        while (!string.IsNullOrEmpty(directory))
        {
            if (ExcludedDirectoryNames.Contains(Path.GetFileName(directory)))
            {
                return true;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return false;
    }

    private static IEnumerable<Type> SafeGetTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null)!;
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PROJECT_SPEC.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root (PROJECT_SPEC.md).");
    }
}