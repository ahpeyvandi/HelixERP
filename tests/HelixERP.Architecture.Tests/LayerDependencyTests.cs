using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Platform;

namespace HelixERP.Architecture.Tests;

public sealed class LayerDependencyTests
{
    private static readonly string[] Modules = ["Sales", "Inventory", "Accounting"];

    [Theory]
    [InlineData("HelixERP.BuildingBlocks.Domain")]
    [InlineData("HelixERP.Modules.Sales.Domain")]
    [InlineData("HelixERP.Modules.Inventory.Domain")]
    [InlineData("HelixERP.Modules.Accounting.Domain")]
    public void Domain_projects_do_not_reference_outer_layers_or_persistence(string assemblyName)
    {
        var forbidden = new List<string>
        {
            "HelixERP.BuildingBlocks.Application",
            "HelixERP.BuildingBlocks.Infrastructure",
            "HelixERP.Shell",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Microsoft.Data"
        };

        if (assemblyName.StartsWith("HelixERP.Modules.", StringComparison.Ordinal))
        {
            var module = assemblyName.Split('.')[2];

            forbidden.AddRange(new[] { "Application", "Infrastructure", "Api" }
                .Select(layer => $"HelixERP.Modules.{module}.{layer}"));

            forbidden.AddRange(Modules
                .Where(other => other != module)
                .Select(other => $"HelixERP.Modules.{other}."));
        }
        else
        {
            forbidden.Add("HelixERP.Modules.");
        }

        AssertNoReferences(assemblyName, forbidden);
    }

    [Fact]
    public void Shared_is_a_leaf_project()
    {
        AssertNoReferences(
            "HelixERP.BuildingBlocks.Shared",
            ["HelixERP.", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Data"]);
    }

    [Theory]
    [InlineData("HelixERP.BuildingBlocks.Application")]
    [InlineData("HelixERP.Shell.Application")]
    [InlineData("HelixERP.Modules.Sales.Application")]
    [InlineData("HelixERP.Modules.Inventory.Application")]
    [InlineData("HelixERP.Modules.Accounting.Application")]
    public void Application_projects_do_not_reference_infrastructure_persistence_or_apis(string assemblyName)
    {
        var forbidden = new List<string>
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Microsoft.Data"
        };

        if (assemblyName.StartsWith("HelixERP.Modules.", StringComparison.Ordinal))
        {
            var module = assemblyName.Split('.')[2];
            forbidden.Add($"HelixERP.Modules.{module}.Infrastructure");
            forbidden.Add($"HelixERP.Modules.{module}.Api");
            forbidden.AddRange(Modules.Where(other => other != module).Select(other => $"HelixERP.Modules.{other}."));
        }
        else
        {
            forbidden.Add("HelixERP.BuildingBlocks.Infrastructure");
            forbidden.Add("HelixERP.Modules.");
            forbidden.Add("HelixERP.Shell.");
        }

        AssertNoReferences(assemblyName, forbidden);
    }

    [Theory]
    [InlineData("HelixERP.BuildingBlocks.Infrastructure")]
    [InlineData("HelixERP.Shell.Infrastructure")]
    [InlineData("HelixERP.Modules.Sales.Infrastructure")]
    [InlineData("HelixERP.Modules.Inventory.Infrastructure")]
    [InlineData("HelixERP.Modules.Accounting.Infrastructure")]
    public void Infrastructure_projects_do_not_reference_apis_or_other_modules(string assemblyName)
    {
        var forbidden = new List<string>
        {
            "Microsoft.AspNetCore"
        };

        if (assemblyName.StartsWith("HelixERP.Modules.", StringComparison.Ordinal))
        {
            var module = assemblyName.Split('.')[2];
            forbidden.Add($"HelixERP.Modules.{module}.Api");
            forbidden.AddRange(Modules.Where(other => other != module).Select(other => $"HelixERP.Modules.{other}."));
        }
        else if (assemblyName == "HelixERP.BuildingBlocks.Infrastructure")
        {
            forbidden.Add("HelixERP.Modules.");
            forbidden.Add("HelixERP.Shell.");
        }

        AssertNoReferences(assemblyName, forbidden);
    }

    [Fact]
    public void Modules_are_isolated_from_each_other()
    {
        foreach (var module in Modules)
        {
            foreach (var layer in new[] { "Domain", "Application", "Infrastructure", "Api" })
            {
                var forbidden = Modules
                    .Where(other => other != module)
                    .Select(other => $"HelixERP.Modules.{other}.")
                    .ToArray();

                AssertNoReferences($"HelixERP.Modules.{module}.{layer}", forbidden);
            }
        }
    }

    [Fact]
    public void Modules_do_not_reference_the_shell()
    {
        foreach (var module in Modules)
        {
            foreach (var layer in new[] { "Domain", "Application", "Infrastructure", "Api" })
            {
                AssertNoReferences($"HelixERP.Modules.{module}.{layer}", ["HelixERP.Shell"]);
            }
        }
    }

    [Fact]
    public void BuildingBlocks_do_not_reference_modules_or_shell()
    {
        foreach (var assembly in new[]
                 {
                     "HelixERP.BuildingBlocks.Domain",
                     "HelixERP.BuildingBlocks.Shared",
                     "HelixERP.BuildingBlocks.Application",
                     "HelixERP.BuildingBlocks.Infrastructure"
                 })
        {
            AssertNoReferences(assembly, ["HelixERP.Modules.", "HelixERP.Shell"]);
        }
    }

    [Fact]
    public void No_source_project_references_an_api_assembly_except_its_own_startup()
    {
        var apiAssemblies = SourceAssemblies.Names.Where(name => name.EndsWith(".Api", StringComparison.Ordinal)).ToArray();

        foreach (var assemblyName in SourceAssemblies.Names.Where(name => !name.EndsWith(".Api", StringComparison.Ordinal)))
        {
            foreach (var apiAssembly in apiAssemblies)
            {
                ArchitectureProbe.References(SourceAssemblies.Load(assemblyName), apiAssembly)
                    .Should().BeFalse($"{assemblyName} must not depend on {apiAssembly}");
            }
        }
    }

    private static void AssertNoReferences(string assemblyName, IEnumerable<string> forbiddenPrefixes)
    {
        var assembly = SourceAssemblies.Load(assemblyName);
        var referencedTypes = ArchitectureProbe.ReferencedTypes(assembly);
        var violations = referencedTypes
            .Where(typeName => forbiddenPrefixes.Any(prefix => typeName.StartsWith(prefix, StringComparison.Ordinal)))
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToList();

        violations.Should().BeEmpty($"{assemblyName} must respect Clean Architecture layering");
    }
}