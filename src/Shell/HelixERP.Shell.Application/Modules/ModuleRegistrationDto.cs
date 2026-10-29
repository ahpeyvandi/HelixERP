namespace HelixERP.Shell.Application.Modules;

public sealed record ModuleRegistrationDto(
    Guid Id,
    string ModuleKey,
    string DisplayName,
    string Host,
    int Port,
    bool IsActive,
    string BaseUrl,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);