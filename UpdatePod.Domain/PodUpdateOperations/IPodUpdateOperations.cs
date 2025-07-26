namespace UpdatePod.Domain.PodUpdateOperations;

public interface IPodUpdateOperations
{
    public Task UpdatePodImage(string nameSpace, string podNameStarts, string? containerName,
        CancellationToken cancellationToken = default);
}