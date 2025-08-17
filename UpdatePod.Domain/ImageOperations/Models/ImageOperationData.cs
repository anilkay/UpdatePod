namespace UpdatePod.Domain.ImageOperations.Models;

public record ImageOperationData(
    bool? UseHarbor = null,
    string? HarborRobotUser = null,
    string? HarborRobotToken = null,
    int? ImageOperationsTimeout = null,
    string? DockerHubToken = null)
{
    internal bool IsHarborUsed() => UseHarbor ?? false;

    internal int GetImageOperationsTimeout() => ImageOperationsTimeout ?? 30;
}