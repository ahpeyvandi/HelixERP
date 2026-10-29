using HelixERP.BuildingBlocks.Shared.Abstractions;

namespace HelixERP.BuildingBlocks.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}