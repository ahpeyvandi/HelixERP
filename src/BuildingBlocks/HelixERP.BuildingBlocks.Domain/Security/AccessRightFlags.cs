namespace HelixERP.BuildingBlocks.Domain.Security;

[Flags]
public enum AccessRightFlags
{
    None = 0,
    Read = 1,
    Write = 2,
    Create = 4,
    Unlink = 8,
    All = Read | Write | Create | Unlink
}