namespace UpdatePod.Domain.ImageOperations;

public class ImageOperationData
{
    public bool? UseHarbor { get; init; }
    public string? HarborRobotUser { get; init; }
    public string? HarborRobotToken { get; init; }
    
    public int? ImageOperationsTimeout { get; init; }
    
    public string? DockerHubToken { get; init; }

    internal bool IsHarborUsed()
    {
        return UseHarbor ?? false;
    }

    internal int GetImageOperationsTimeout()
    {
        return ImageOperationsTimeout ?? 30;
    }
    
}