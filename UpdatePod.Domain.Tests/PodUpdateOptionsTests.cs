using UpdatePod.Domain.PodUpdateOperations;

namespace UpdatePod.Domain.Tests;

public class PodUpdateOptionsTests
{
    [Fact]
    public void FromEnvironment_ReadsAndTrimsValues()
    {
        var env = new Dictionary<string, string?>
        {
            ["POD_NAMESPACE"] = " default ",
            ["POD_NAME_PREFIX"] = " my-app ",
            ["POD_CONTAINER_NAME"] = " web ",
            ["RESTART_INTERVAL_MINUTES"] = "5"
        };

        var options = PodUpdateOptions.FromEnvironment(Get(env));

        Assert.Equal("default", options.Namespace);
        Assert.Equal("my-app", options.PodNamePrefix);
        Assert.Equal("web", options.ContainerName);
        Assert.Equal(5, options.IntervalMinutes);
    }

    [Fact]
    public void FromEnvironment_UsesDefaultsAndNullForBlankContainer()
    {
        var env = new Dictionary<string, string?>
        {
            ["POD_NAMESPACE"] = "default",
            ["POD_NAME_PREFIX"] = "my-app",
            ["POD_CONTAINER_NAME"] = "   "
        };

        var options = PodUpdateOptions.FromEnvironment(Get(env));

        Assert.Equal(10, options.IntervalMinutes);
        Assert.Null(options.ContainerName);
    }

    [Theory]
    [InlineData("POD_NAMESPACE")]
    [InlineData("POD_NAME_PREFIX")]
    public void FromEnvironment_ThrowsWhenRequiredValueIsMissing(string missingName)
    {
        var env = new Dictionary<string, string?>
        {
            ["POD_NAMESPACE"] = "default",
            ["POD_NAME_PREFIX"] = "my-app"
        };
        env.Remove(missingName);

        var ex = Assert.Throws<InvalidOperationException>(
            () => PodUpdateOptions.FromEnvironment(Get(env)));

        Assert.Contains(missingName, ex.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void FromEnvironment_ThrowsWhenIntervalIsInvalid(string value)
    {
        var env = new Dictionary<string, string?>
        {
            ["POD_NAMESPACE"] = "default",
            ["POD_NAME_PREFIX"] = "my-app",
            ["RESTART_INTERVAL_MINUTES"] = value
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => PodUpdateOptions.FromEnvironment(Get(env)));

        Assert.Contains("RESTART_INTERVAL_MINUTES", ex.Message);
    }

    private static Func<string, string?> Get(Dictionary<string, string?> env)
        => name => env.TryGetValue(name, out var value) ? value : null;
}
