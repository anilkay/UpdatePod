namespace UpdatePod.Domain.ImageOperations;

public class ImageOperationData
{
    public bool? UseHarbor { get; init; }
    public string? HarborRobotUser { get; init; }
    public string? HarborRobotToken { get; init; }
}