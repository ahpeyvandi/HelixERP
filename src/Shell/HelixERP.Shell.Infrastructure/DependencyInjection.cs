using HelixERP.BuildingBlocks.Application.Persistence;
using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Application.Storage;
using HelixERP.Shell.Application.Bootstrap;
using HelixERP.Shell.Application.Modules;
using HelixERP.Shell.Application.Security;
using HelixERP.Shell.Application.Storage;
using HelixERP.Shell.Infrastructure.Bootstrap;
using HelixERP.Shell.Infrastructure.Modules;
using HelixERP.Shell.Infrastructure.Persistence;
using HelixERP.Shell.Infrastructure.Security;
using HelixERP.Shell.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HelixERP.Shell.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShellInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HelixDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'HelixDb' is not configured. Set 'ConnectionStrings:HelixDb'.");
        }

        services.AddDbContext<ShellDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServer => sqlServer.MigrationsAssembly(typeof(ShellDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<ShellDbContext>());

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        jwtOptions.Validate();
        services.AddSingleton<IOptions<JwtOptions>>(Options.Create(jwtOptions));

        var bootstrapOptions = configuration.GetSection(BootstrapOptions.SectionName).Get<BootstrapOptions>()
            ?? new BootstrapOptions();
        services.AddSingleton<IOptions<BootstrapOptions>>(Options.Create(bootstrapOptions));

        services.AddOptions<FileStorageOptions>().Configure(options =>
            configuration.GetSection(FileStorageOptions.SectionName).Bind(options));

        services.AddScoped<IModuleRegistry, ModuleRegistry>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ShellDbInitializer>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        services.AddSingleton<IPasswordHasher>(_ => new Pbkdf2PasswordHasher());
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}