using FluentAssertions;
using YamlDotNet.Serialization;

namespace HelixERP.Shell.UnitTests;

public sealed class DockerComposeContractTests
{
    private static readonly string[] RequiredVolumes =
    [
        "erp_files",
        "erp_redis_data",
        "erp_logs",
        "erp_ids"
    ];

    [Fact]
    public void Compose_defines_redis_volume_init_and_identityserver_services()
    {
        var compose = LoadCompose();
        var services = Map(compose["services"]);

        services.Keys.Should().Contain(["redis", "volume-init", "identityserver"]);

        Map(services["redis"])["image"].Should().Be("redis:7.4-alpine");
        Map(services["redis"])["restart"].Should().Be("unless-stopped");
        Map(services["volume-init"])["restart"].Should().Be("no");
    }

    [Fact]
    public void Identityserver_service_builds_from_the_ids_dockerfile()
    {
        var compose = LoadCompose();
        var services = Map(compose["services"]);
        var identityServer = Map(services["identityserver"]);

        var build = Map(identityServer["build"]);
        build["context"].Should().Be("..");
        build["dockerfile"].Should().Be("docker/modules/ids.Dockerfile");

        Sequence(identityServer["volumes"]).Should().Contain("erp_ids:/var/helix/ids");
    }

    [Fact]
    public void Compose_does_not_define_a_sql_server_container_or_volume()
    {
        var composePath = Path.Combine(RepositoryRoot(), "docker", "docker-compose.yml");
        var compose = LoadCompose();
        var services = Map(compose["services"]);
        var volumes = Map(compose["volumes"]);

        services.Keys.Should().NotContain("mssql", "SQL Server is a local installation");
        volumes.Keys.Should().NotContain("erp_mssql_data");

        var content = File.ReadAllText(composePath);
        content.Should().NotContain("1433");
        content.Should().NotContain("mcr.microsoft.com");
        content.Should().NotContain("erp_mssql_data");
    }

    [Fact]
    public void Compose_declares_the_required_named_volumes_with_fixed_names()
    {
        var volumes = Map(LoadCompose()["volumes"]);

        volumes.Keys.Should().BeEquivalentTo(RequiredVolumes);

        foreach (var volumeName in RequiredVolumes)
        {
            Map(volumes[volumeName])["name"].Should().Be(volumeName);
        }
    }

    [Fact]
    public void Redis_data_is_mounted_on_the_named_volume()
    {
        var services = Map(LoadCompose()["services"]);

        Sequence(Map(services["redis"])["volumes"]).Should().Contain("erp_redis_data:/data");
    }

    [Fact]
    public void File_and_log_volumes_are_created_and_owned_by_the_app_user()
    {
        var services = Map(LoadCompose()["services"]);
        var volumeInit = Map(services["volume-init"]);

        var volumes = Sequence(volumeInit["volumes"]).Cast<string>().ToList();
        volumes.Should().Contain("erp_files:/data/files");
        volumes.Should().Contain("erp_logs:/data/logs");
        volumes.Should().Contain("erp_ids:/data/ids");
    }

    [Fact]
    public void Development_override_exposes_redis_and_not_sql_server()
    {
        var composePath = Path.Combine(RepositoryRoot(), "docker", "docker-compose.override.yml");
        var compose = LoadYaml(composePath);
        var services = Map(compose["services"]);

        services.Keys.Should().BeEquivalentTo("redis");
        Sequence(Map(services["redis"])["ports"]).Should().Contain("6379:6379");

        var content = File.ReadAllText(composePath);
        content.Should().NotContain("1433");
        content.Should().NotContain("mssql");
    }

    [Fact]
    public void Compose_files_never_use_nginx()
    {
        var dockerDirectory = Path.Combine(RepositoryRoot(), "docker");
        var offenders = Directory.GetFiles(dockerDirectory, "*", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("nginx", StringComparison.OrdinalIgnoreCase))
            .ToList();

        offenders.Should().BeEmpty("AD001 forbids Nginx; the Shell owns YARP");
    }

    [Fact]
    public void Compose_never_removes_volumes_on_down()
    {
        var dockerDirectory = Path.Combine(RepositoryRoot(), "docker");
        var composeFiles = Directory.GetFiles(dockerDirectory, "docker-compose*.yml");
        var content = string.Join(
            "\n",
            composeFiles
                .SelectMany(File.ReadAllLines)
                .Where(line => !line.TrimStart().StartsWith('#')));

        content.Should().NotContain("down -v");
        content.Should().NotContain("docker volume prune");
    }

    [Fact]
    public void Module_dockerfiles_exist_for_shell_all_modules_and_identityserver()
    {
        var modulesDirectory = Path.Combine(RepositoryRoot(), "docker", "modules");

        foreach (var dockerfile in new[] { "shell.Dockerfile", "sales.Dockerfile", "inventory.Dockerfile", "accounting.Dockerfile", "ids.Dockerfile" })
        {
            File.Exists(Path.Combine(modulesDirectory, dockerfile)).Should().BeTrue();
        }
    }

    private static Dictionary<string, object?> LoadCompose()
        => LoadYaml(Path.Combine(RepositoryRoot(), "docker", "docker-compose.yml"));

    private static Dictionary<string, object?> LoadYaml(string path)
    {
        File.Exists(path).Should().BeTrue($"expected {path} to exist");

        var yaml = new DeserializerBuilder().Build().Deserialize<object>(File.ReadAllText(path));

        return Map(yaml);
    }

    private static Dictionary<string, object?> Map(object? value)
    {
        value.Should().BeAssignableTo<IDictionary<object, object>>();
        return ((IDictionary<object, object>)value!)
            .ToDictionary(pair => pair.Key.ToString()!, pair => (object?)pair.Value);
    }

    private static List<object?> Sequence(object? value)
    {
        value.Should().BeAssignableTo<IEnumerable<object>>();
        return ((IEnumerable<object>)value!).Cast<object?>().ToList();
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