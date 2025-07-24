using System.Data;
using k8s;
using k8s.Models;

namespace UpdatePod.Domain.KubernetesUtils;

public class KubernetesOperationWithKubernetesClient: IKubernetesOperations
{
    
    private readonly Kubernetes _client;

    public KubernetesOperationWithKubernetesClient()
    {
        KubernetesClientConfiguration configuration;
        try
        {
            configuration = KubernetesClientConfiguration.InClusterConfig();
        }
        catch
        {
            configuration = KubernetesClientConfiguration.BuildDefaultConfig();
        }
        
        _client = new Kubernetes(configuration);
    }

    public async Task<string> GetPodContainerImageInfo(string namespaceInfo, string podNameStarts, string? containerName = null,CancellationToken ct=default)
    {
        var pods = await _client.ListNamespacedPodAsync(namespaceInfo,cancellationToken:ct);
        var matchedPod = pods.Items
            .FirstOrDefault(p => p.Metadata?.Name != null && p.Metadata.Name.StartsWith(podNameStarts));

        if (matchedPod == null)
        {
            throw new ConstraintException($"No pod found starting with '{podNameStarts}' in namespace '{namespaceInfo}'.");
        }

        if (containerName is null)
        {
            return matchedPod.Spec.Containers.First().Image;
        }

        var container = matchedPod.Spec.Containers
            .FirstOrDefault(c => c.Name == containerName);

        if (container is null)
        {
            throw new ConstraintException($"Container '{containerName}' not found in pod '{matchedPod.Metadata.Name}'.");
        }

        return container.Image;
    }
    public async Task<string?> GetPodContainerImageHash(string namespaceInfo, string podNameStarts, string? containerName = null,CancellationToken ct=default)
    {
        var pods = await _client.ListNamespacedPodAsync(namespaceInfo,cancellationToken:ct);
        var matchedPod = pods.Items
            .FirstOrDefault(p => p.Metadata?.Name != null && p.Metadata.Name.StartsWith(podNameStarts));

        if (matchedPod == null)
        {
            throw new ConstraintException($"No pod found starting with '{podNameStarts}' in namespace '{namespaceInfo}'.");
        }

        V1ContainerStatus? targetContainer;

        if (containerName is null)
        {
            targetContainer = matchedPod.Status.ContainerStatuses.FirstOrDefault();
        }
        else
        {
            targetContainer = matchedPod.Status.ContainerStatuses
                .FirstOrDefault(c => c.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));
        }

        if (targetContainer == null)
        {
            throw new ConstraintException($"Container '{containerName}' not found in pod '{matchedPod.Metadata.Name}'.");
        }

        return GetImageWithDigest(targetContainer.ImageID);
    }
    

    public async Task<string> GetImagePullPolicy(string namespaceInfo, string podNameStarts, string? containerName = null,CancellationToken ct=default)
    {
        var pods = await _client.ListNamespacedPodAsync(namespaceInfo,cancellationToken:ct);
        var matchedPod = pods.Items
            .FirstOrDefault(p => p.Metadata?.Name != null && p.Metadata.Name.StartsWith(podNameStarts));

        if (matchedPod == null)
        {
            throw new ConstraintException($"No pod found starting with '{podNameStarts}' in namespace '{namespaceInfo}'.");
        }

        var targetContainer = containerName is null
            ? matchedPod.Spec.Containers.FirstOrDefault()
            : matchedPod.Spec.Containers.FirstOrDefault(c => c.Name == containerName);

        if (targetContainer == null)
        {
            throw new ConstraintException($"Container '{containerName}' not found in pod '{matchedPod.Metadata.Name}'.");
        }

        return targetContainer.ImagePullPolicy ?? "(default - IfNotPresent)";
    }

    public async Task<string> GetDeploymentFromPod(string namespaceInfo, string podNameStarts, CancellationToken ct=default)
    {
        var pods = await _client.ListNamespacedPodAsync(namespaceInfo, cancellationToken:ct);
        var matchedPod = pods.Items
            .FirstOrDefault(p => p.Metadata?.Name != null && p.Metadata.Name.StartsWith(podNameStarts));

        if (matchedPod == null)
        {
            throw new ConstraintException($"No pod found starting with '{podNameStarts}' in namespace '{namespaceInfo}'.");
        }

        var rsName = matchedPod.Metadata.OwnerReferences?.FirstOrDefault(r => r.Kind == "ReplicaSet")?.Name;
        if (rsName is null)
        {
            throw new KubernetesObjectNotFoundException($"No ReplicaSet found for pod '{matchedPod.Metadata.Name}'.");
        }

        var rs = await _client.ReadNamespacedReplicaSetAsync(rsName, namespaceInfo, cancellationToken:ct);
        var deploymentName = rs.Metadata.OwnerReferences?.FirstOrDefault(d => d.Kind == "Deployment")?.Name;

        if (deploymentName is null)
        {
            throw new KubernetesObjectNotFoundException($"No Deployment found for ReplicaSet '{rsName}'.");
        }

        return deploymentName;
    }

    public async  Task<string> GetStateFulSetFromPod(string namespaceInfo, string podNameStarts, CancellationToken ct = default)
    {
        var pods = await _client.ListNamespacedPodAsync(namespaceInfo, cancellationToken:ct);
        var matchedPod = pods.Items
            .FirstOrDefault(p => p.Metadata?.Name != null && p.Metadata.Name.StartsWith(podNameStarts));

        if (matchedPod == null)
        {
            throw new ConstraintException($"No pod found starting with '{podNameStarts}' in namespace '{namespaceInfo}'.");
        }
        
        var stName = matchedPod.Metadata.OwnerReferences?.FirstOrDefault(r => r.Kind == "StatefulSet")?.Name;
        
        if (stName is null)
        {
            throw new Exception($"No StateFulSet found for pod '{matchedPod.Metadata.Name}'.");
        }
        
        return stName;
    }

    public async Task<bool> RestartDeployment(string namespaceInfo, string deployment, CancellationToken ct=default)
    {
        var patchObj = new
        {
            spec = new
            {
                template = new
                {
                    metadata = new
                    {
                        annotations = new Dictionary<string, string>
                        {
                            ["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("o")
                        }
                    }
                }
            }
        };

        var patch = new V1Patch(patchObj, V1Patch.PatchType.StrategicMergePatch);

        await _client.PatchNamespacedDeploymentAsync(patch, deployment, namespaceInfo, cancellationToken:ct);
        return true;
    }
    public async Task<bool> RestartStateFulSet(string namespaceInfo, string statefulSetName, CancellationToken ct = default)
    {
        var patchObj = new
        {
            spec = new
            {
                template = new
                {
                    metadata = new
                    {
                        annotations = new Dictionary<string, string>
                        {
                            ["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("o")
                        }
                    }
                }
            }
        };
        
        var patch = new V1Patch(patchObj, V1Patch.PatchType.StrategicMergePatch);
        
        await _client.PatchNamespacedStatefulSetAsync(
            name: statefulSetName,
            namespaceParameter: namespaceInfo,
            body: patch,
            cancellationToken: ct
        );

        return true;
    }

    private string? GetImageWithDigest(string imageId)
    {
            var index = imageId.IndexOf("@sha256:", StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                throw new ConstraintException($"Image {imageId} does not exist");
            }
            return index >= 0 ? imageId.Substring(index + 1) : null;
    }
}