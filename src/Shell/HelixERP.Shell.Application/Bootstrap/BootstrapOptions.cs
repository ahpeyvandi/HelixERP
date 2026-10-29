namespace HelixERP.Shell.Application.Bootstrap;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public bool MigrateDatabase { get; set; } = true;

    public bool SeedOnStartup { get; set; } = true;

    public string AdminLogin { get; set; } = "admin";

    public string AdminEmail { get; set; } = "admin@helix.local";

    public string AdminDisplayName { get; set; } = "Administrator";

    public string? AdminPassword { get; set; }
}