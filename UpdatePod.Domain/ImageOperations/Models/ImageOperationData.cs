using UpdatePod.Domain.Configuration;

namespace UpdatePod.Domain.ImageOperations.Models;

public record ImageOperationData(
    bool? UseHarbor = null,
    string? HarborRobotUser = null,
    string? HarborRobotToken = null,
    int? ImageOperationsTimeout = null,
    string? DockerHubToken = null)
{
    private const int DefaultImageOperationsTimeout = 30;

    public static ImageOperationData FromEnvironment(Func<string, string?>? getEnvironmentVariable = null)
    {
        return new ImageOperationData(
            UseHarbor: EnvironmentConfiguration.GetOptionalBool("USE_HARBOR", getEnvironmentVariable),
            HarborRobotUser: EnvironmentConfiguration.GetOptional("HARBOR_ROBOT_USER", getEnvironmentVariable),
            HarborRobotToken: EnvironmentConfiguration.GetOptional("HARBOR_ROBOT_TOKEN", getEnvironmentVariable),
            ImageOperationsTimeout: EnvironmentConfiguration.GetOptionalPositiveInt("IMAGE_OPERATIONS_TIMEOUT", getEnvironmentVariable),
            DockerHubToken: EnvironmentConfiguration.GetOptional("DOCKER_HUB_TOKEN", getEnvironmentVariable));
    }

    internal bool IsHarborUsed() => UseHarbor ?? false;

    internal int GetImageOperationsTimeout() => ImageOperationsTimeout ?? DefaultImageOperationsTimeout;
}
