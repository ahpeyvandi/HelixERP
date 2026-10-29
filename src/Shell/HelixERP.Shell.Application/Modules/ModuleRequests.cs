namespace HelixERP.Shell.Application.Modules;

public sealed record RegisterModuleRequest(
    string ModuleKey,
    string DisplayName,
    string Host,
    int Port);

public sealed record UpdateModuleEndpointRequest(string Host, int Port);