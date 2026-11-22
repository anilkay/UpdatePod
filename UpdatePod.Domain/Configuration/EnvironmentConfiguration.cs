using System.Globalization;

namespace UpdatePod.Domain.Configuration;

public static class EnvironmentConfiguration
{
    /// <summary>
    /// Longest duration accepted by <see cref="Task.Delay(TimeSpan)"/> and
    /// <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/> (~49.7 days).
    /// </summary>
    public static readonly TimeSpan MaxTimerDuration = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    public static string GetRequired(string name, Func<string, string?>? getEnvironmentVariable = null)
    {
        var value = Read(name, getEnvironmentVariable);
        if (value is null)
        {
            throw new InvalidOperationException($"{name} environment variable is required.");
        }

        return value;
    }

    public static string? GetOptional(string name, Func<string, string?>? getEnvironmentVariable = null)
        => Read(name, getEnvironmentVariable);

    public static int GetPositiveInt(string name, int defaultValue, Func<string, string?>? getEnvironmentVariable = null,
        int maxValue = int.MaxValue)
    {
        var value = Read(name, getEnvironmentVariable);
        return value is null ? defaultValue : ParsePositiveInt(name, value, maxValue);
    }

    public static int? GetOptionalPositiveInt(string name, Func<string, string?>? getEnvironmentVariable = null,
        int maxValue = int.MaxValue)
    {
        var value = Read(name, getEnvironmentVariable);
        return value is null ? null : ParsePositiveInt(name, value, maxValue);
    }

    public static bool? GetOptionalBool(string name, Func<string, string?>? getEnvironmentVariable = null)
    {
        var value = Read(name, getEnvironmentVariable);
        if (value is null)
        {
            return null;
        }

        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"{name} must be either 'true' or 'false'. Current value: '{value}'.");
        }

        return parsed;
    }

    private static string? Read(string name, Func<string, string?>? getEnvironmentVariable)
    {
        var value = (getEnvironmentVariable ?? Environment.GetEnvironmentVariable)(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int ParsePositiveInt(string name, string value, int maxValue)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 1)
        {
            throw new InvalidOperationException($"{name} must be a positive integer. Current value: '{value}'.");
        }

        if (parsed > maxValue)
        {
            throw new InvalidOperationException($"{name} must not be greater than {maxValue}. Current value: '{value}'.");
        }

        return parsed;
    }
}
