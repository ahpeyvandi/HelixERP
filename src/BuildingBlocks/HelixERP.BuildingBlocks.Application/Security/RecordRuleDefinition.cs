namespace HelixERP.BuildingBlocks.Application.Security;

public sealed record RecordRuleDefinition(
    Guid Id,
    string Name,
    string Model,
    string Domain,
    bool IsGlobal,
    bool IsActive,
    Guid? CompanyId);