namespace HelixERP.BuildingBlocks.Shared.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}