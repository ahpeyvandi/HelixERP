using System.Reflection;

namespace HelixERP.Architecture.Tests;

internal static class SourceAssemblies
{
    public static readonly string[] Names =
    [
        "HelixERP.BuildingBlocks.Domain",
        "HelixERP.BuildingBlocks.Shared",
        "HelixERP.BuildingBlocks.Application",
        "HelixERP.BuildingBlocks.Infrastructure",
        "HelixERP.Shell.Application",
        "HelixERP.Shell.Infrastructure",
        "HelixERP.Shell.Api",
        "HelixERP.Modules.Sales.Domain",
        "HelixERP.Modules.Sales.Application",
        "HelixERP.Modules.Sales.Infrastructure",
        "HelixERP.Modules.Sales.Api",
        "HelixERP.Modules.Inventory.Domain",
        "HelixERP.Modules.Inventory.Application",
        "HelixERP.Modules.Inventory.Infrastructure",
        "HelixERP.Modules.Inventory.Api",
        "HelixERP.Modules.Accounting.Domain",
        "HelixERP.Modules.Accounting.Application",
        "HelixERP.Modules.Accounting.Infrastructure",
        "HelixERP.Modules.Accounting.Api"
    ];

    public static Assembly Load(string name) => Assembly.Load(name);
}