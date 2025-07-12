namespace UpdatePod.Domain.KubernetesUtils;

public interface IKubernetesOperations
{
    public Task<string> GetPodContainerImageInfo(string namespaceInfo,string podNameStarts, string? containerName=null, CancellationToken ct = default);
    public Task<string?> GetPodContainerImageHash(string namespaceInfo,string podNameStarts, string? containerName=null, CancellationToken ct = default);
    
    public Task<string> GetImagePullPolicy(string namespaceInfo, string podNameStarts, string? containerName = null, CancellationToken ct = default);
    
    public Task<string> GetDeploymentFromPod(string namespaceInfo, string podNameStarts,CancellationToken ct = default);
    
    public Task<bool> RestartDeployment(string namespaceInfo, string deployment, CancellationToken ct = default);
    
}