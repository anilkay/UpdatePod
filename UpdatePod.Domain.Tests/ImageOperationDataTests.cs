using UpdatePod.Domain.ImageOperations.Models;

namespace UpdatePod.Domain.Tests;

public class ImageOperationDataTests
{
    [Fact]
    public void FromEnvironment_ReadsAndTrimsValues()
    {
        var env = new Dictionary<string, string?>
        {
            ["USE_HARBOR"] = "true",
            ["HARBOR_ROBOT_USER"] = " robot$project ",
            ["HARBOR_ROBOT_TOKEN"] = " secret ",
            ["IMAGE_OPERATIONS_TIMEOUT"] = "45",
            ["DOCKER_HUB_TOKEN"] = " jwt "
        };

        var data = ImageOperationData.FromEnvironment(Get(env));

        Assert.True(data.UseHarbor!.Value);
        Assert.Equal("robot$project", data.HarborRobotUser);
        Assert.Equal("secret", data.HarborRobotToken);
        Assert.Equal(45, data.ImageOperationsTimeout!.Value);
        Assert.Equal("jwt", data.DockerHubToken);
    }

    [Fact]
    public void FromEnvironment_ReturnsNullsWhenUnset()
    {
        var data = ImageOperationData.FromEnvironment(_ => null);

        Assert.Null(data.UseHarbor);
        Assert.Null(data.HarborRobotUser);
        Assert.Null(data.HarborRobotToken);
        Assert.Null(data.ImageOperationsTimeout);
        Assert.Null(data.DockerHubToken);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("1")]
    public void FromEnvironment_ThrowsWhenUseHarborIsInvalid(string value)
    {
        var env = new Dictionary<string, string?> { ["USE_HARBOR"] = value };

        var ex = Assert.Throws<InvalidOperationException>(
            () => ImageOperationData.FromEnvironment(Get(env)));

        Assert.Contains("USE_HARBOR", ex.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public void FromEnvironment_ThrowsWhenTimeoutIsInvalid(string value)
    {
        var env = new Dictionary<string, string?> { ["IMAGE_OPERATIONS_TIMEOUT"] = value };

        var ex = Assert.Throws<InvalidOperationException>(
            () => ImageOperationData.FromEnvironment(Get(env)));

        Assert.Contains("IMAGE_OPERATIONS_TIMEOUT", ex.Message);
    }

    private static Func<string, string?> Get(Dictionary<string, string?> env)
        => name => env.TryGetValue(name, out var value) ? value : null;
}
