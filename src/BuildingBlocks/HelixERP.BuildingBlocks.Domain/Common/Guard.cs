using HelixERP.BuildingBlocks.Domain.Exceptions;

namespace HelixERP.BuildingBlocks.Domain.Common;

public static class Guard
{
    public static T NotNull<T>(T? value, string parameterName)
        where T : class
        => value ?? throw new ArgumentNullException(parameterName);

    public static string NotNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }

        return value;
    }

    public static int InRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value must be between {minimum} and {maximum}.");
        }

        return value;
    }

    public static void Against(bool condition, string message)
    {
        if (condition)
        {
            throw new DomainException(message);
        }
    }
}