namespace UpdatePod.Domain.ImageOperations;

public interface IImageOperationsStrategy
{
    public Task<string?> GetLatestHash(string repository, string tag, CancellationToken token);
}