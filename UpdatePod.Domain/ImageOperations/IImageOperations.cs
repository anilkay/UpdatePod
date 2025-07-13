namespace UpdatePod.Domain.ImageOperations;

public interface IImageOperations
{
    public Task<string?> GetLatestHashFromImage(string image, CancellationToken ct = default);
}