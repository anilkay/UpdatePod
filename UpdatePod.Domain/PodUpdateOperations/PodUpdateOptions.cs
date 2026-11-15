using UpdatePod.Domain.Configuration;

namespace UpdatePod.Domain.PodUpdateOperations;

public sealed record PodUpdateOptions(
    string Namespace,
    string PodNamePrefix,
    string? ContainerName,
    int IntervalMinutes)
{
    public static PodUpdateOptions FromEnvironment(Func<string, string?>? getEnvironmentVariable = null)
    {
        return new PodUpdateOptions(
            Namespace: EnvironmentConfiguration.GetRequired("POD_NAMESPACE", getEnvironmentVariable),
            PodNamePrefix: EnvironmentConfiguration.GetRequired("POD_NAME_PREFIX", getEnvironmentVariable),
            ContainerName: EnvironmentConfiguration.GetOptional("POD_CONTAINER_NAME", getEnvironmentVariable),
            IntervalMinutes: EnvironmentConfiguration.GetPositiveInt("RESTART_INTERVAL_MINUTES", 10, getEnvironmentVariable));
    }
}
