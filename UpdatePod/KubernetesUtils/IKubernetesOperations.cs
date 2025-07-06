namespace UpdatePod.KubernetesUtils;

public interface IKubernetesOperations
{
    public Task<string> GetPodContainerImageInfo(string namespaceInfo,string podNameStarts, string? containerName=null);
    public Task<string?> GetPodContainerImageHash(string namespaceInfo,string podNameStarts, string? containerName=null);
    
    public Task<string> GetImagePullPolicy(string namespaceInfo, string podNameStarts, string? containerName = null);
    
    public Task<string> GetDeploymentFromPod(string namespaceInfo, string podNameStarts);
    
    public Task<bool> RestartDeployment(string namespaceInfo, string deployment);
    
}