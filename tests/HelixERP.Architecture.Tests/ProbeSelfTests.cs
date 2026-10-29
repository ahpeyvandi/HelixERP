using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Architecture.Tests;

public sealed class ProbeSelfTests
{
#pragma warning disable CS0169
    private DbContext? _deliberateViolation;
#pragma warning restore CS0169

    [Fact]
    public void Probe_detects_a_forbidden_dependency_inside_an_assembly()
    {
        var referencedTypes = ArchitectureProbe.ReferencedTypes(typeof(ProbeSelfTests).Assembly);

        referencedTypes.Should().Contain("Microsoft.EntityFrameworkCore.DbContext");
    }

    [Fact]
    public void Probe_does_not_report_persistence_dependencies_for_the_domain()
    {
        var referencedTypes = ArchitectureProbe.ReferencedTypes(typeof(ModuleRegistration).Assembly);

        referencedTypes.Should().NotContain(typeName => typeName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}