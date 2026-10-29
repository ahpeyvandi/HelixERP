using System.Text.RegularExpressions;
using HelixERP.BuildingBlocks.Domain.Common;
using HelixERP.BuildingBlocks.Domain.Platform.Events;

namespace HelixERP.BuildingBlocks.Domain.Platform;

public sealed class ModuleRegistration : AggregateRoot<Guid>
{
    private static readonly Regex ModuleKeyPattern = new(
        "^[a-z][a-z0-9-]{1,63}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private ModuleRegistration()
    {
    }

    private ModuleRegistration(
        Guid id,
        string moduleKey,
        string displayName,
        string host,
        int port,
        DateTime createdAtUtc)
        : base(id)
    {
        ModuleKey = moduleKey;
        DisplayName = displayName;
        Host = host;
        Port = port;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public string ModuleKey { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Host { get; private set; } = string.Empty;

    public int Port { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public string BaseUrl => Host.Contains(':', StringComparison.Ordinal)
        ? $"http://[{Host}]:{Port}"
        : $"http://{Host}:{Port}";

    public static ModuleRegistration Create(
        string moduleKey,
        string displayName,
        string host,
        int port,
        DateTime createdAtUtc)
    {
        var normalizedKey = NormalizeModuleKey(moduleKey);
        var normalizedHost = NormalizeHost(host);
        var validatedPort = ValidatePort(port);
        var displayNameValue = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim();

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        var registration = new ModuleRegistration(
            Guid.NewGuid(),
            normalizedKey,
            displayNameValue,
            normalizedHost,
            validatedPort,
            createdAtUtc);

        registration.RaiseDomainEvent(
            new ModuleRegisteredDomainEvent(registration.Id, registration.ModuleKey, createdAtUtc));

        return registration;
    }

    public void UpdateEndpoint(string host, int port, DateTime updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Host = NormalizeHost(host);
        Port = ValidatePort(port);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTime updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Deactivate(DateTime updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string NormalizeModuleKey(string moduleKey)
    {
        var value = Guard.NotNullOrWhiteSpace(moduleKey, nameof(moduleKey)).Trim().ToLowerInvariant();

        Guard.Against(
            !ModuleKeyPattern.IsMatch(value),
            $"Module key '{moduleKey}' is invalid. It must match {ModuleKeyPattern}.");

        return value;
    }

    private static string NormalizeHost(string host)
    {
        var value = Guard.NotNullOrWhiteSpace(host, nameof(host)).Trim().TrimEnd('/').ToLowerInvariant();

        Guard.Against(
            value.Contains("://", StringComparison.Ordinal) || value.Contains('/', StringComparison.Ordinal),
            $"Host '{host}' must not contain a scheme or path.");

        Guard.Against(
            Uri.CheckHostName(value) == UriHostNameType.Unknown,
            $"Host '{host}' is not a valid host name or IP address.");

        return value;
    }

    private static int ValidatePort(int port)
    {
        Guard.Against(port is < 1 or > 65535, $"Port '{port}' must be between 1 and 65535.");
        return port;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(
            value.Kind != DateTimeKind.Utc,
            $"{parameterName} must be a UTC timestamp.");
    }
}