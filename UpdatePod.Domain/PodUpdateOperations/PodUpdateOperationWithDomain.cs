using Microsoft.Extensions.Logging;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.KubernetesUtils;

namespace UpdatePod.Domain.PodUpdateOperations;

public class PodUpdateOperationWithDomain : IPodUpdateOperations
{
    private readonly IKubernetesOperations _kubernetesOperations;
    private readonly IImageOperations _imageOperations;
    private readonly ILogger<PodUpdateOperationWithDomain> _logger;

    private const string ImagePullPolicyAlways = "Always";
    private const string ImageNotFoundMessage = "Image {0} has not been found in registry.";
    private const string DeploymentRestartFailedMessage = "Deployment restart failed";
    private const string StatefulSetRestartFailedMessage = "StatefulSet restart failed";

    public PodUpdateOperationWithDomain(IKubernetesOperations kubernetesOperations, IImageOperations imageOperations,
        ILogger<PodUpdateOperationWithDomain> logger)
    {
        _kubernetesOperations = kubernetesOperations;
        _imageOperations = imageOperations;
        _logger = logger;
    }
    
    public async Task UpdatePodImage(string nameSpace, string podNameStarts, string? containerName,
        CancellationToken cancellationToken = default)
    {
        var imagePullPolicy = await GetImagePullPolicy(nameSpace, podNameStarts, containerName, cancellationToken);
        ValidateImagePullPolicy(imagePullPolicy);

        var podImageHash = await GetPodContainerImageHash(nameSpace, podNameStarts, containerName, cancellationToken);
        var containerImageName = await GetPodContainerImageInfo(nameSpace, podNameStarts, containerName, cancellationToken);
        var latestImageHashFromRegistry = await GetLatestImageHash(containerImageName, cancellationToken);

        ValidateLatestImageHash(latestImageHashFromRegistry, containerName);
        LogImageHashes(podImageHash, latestImageHashFromRegistry);

        if (latestImageHashFromRegistry != podImageHash)
        {
            await RestartDeploymentOrStatefulSet(nameSpace, podNameStarts, cancellationToken);
        }
    }

    private async Task<string> GetImagePullPolicy(string nameSpace, string podNameStarts, string? containerName, CancellationToken cancellationToken)
    {
        return await _kubernetesOperations.GetImagePullPolicy(nameSpace, podNameStarts,containerName, cancellationToken);
    }

    private static void ValidateImagePullPolicy(string imagePullPolicy)
    {
        if (imagePullPolicy != ImagePullPolicyAlways)
        {
            throw new ApplicationException($"Image pull policy must be {ImagePullPolicyAlways} but {imagePullPolicy}");
        }
    }

    private async Task<string?> GetPodContainerImageHash(string nameSpace, string podNameStarts, string? containerName, CancellationToken cancellationToken)
    {
        return await _kubernetesOperations.GetPodContainerImageHash(nameSpace, podNameStarts, containerName, cancellationToken);
    }

    private async Task<string> GetPodContainerImageInfo(string nameSpace, string podNameStarts, string? containerName, CancellationToken cancellationToken)
    {
        return await _kubernetesOperations.GetPodContainerImageInfo(nameSpace, podNameStarts, containerName, cancellationToken);
    }

    private async Task<string?> GetLatestImageHash(string containerImageName, CancellationToken cancellationToken)
    {
        return await _imageOperations.GetLatestHashFromImage(containerImageName, cancellationToken);
    }

    private void ValidateLatestImageHash(string? latestImageHashFromRegistry, string? containerName)
    {
        if (latestImageHashFromRegistry is null)
        {
            throw new ApplicationException(string.Format(ImageNotFoundMessage, containerName));
        }
    }

    private void LogImageHashes(string? podImageHash, string? latestImageHashFromRegistry)
    {
        _logger.LogInformation($"Pod Image hash: {podImageHash ?? "null"} Latest Image Hash On registry: {latestImageHashFromRegistry ?? "null"}");
    }

    private async Task RestartDeploymentOrStatefulSet(string nameSpace, string podNameStarts, CancellationToken cancellationToken)
    {
        try
        {
            var deployment = await _kubernetesOperations.GetDeploymentFromPod(nameSpace, podNameStarts, cancellationToken);
            var deploymentResult = await _kubernetesOperations.RestartDeployment(nameSpace, deployment, cancellationToken);

            if (deploymentResult)
            {
                _logger.LogInformation("Deployment restarted");
            }
            else
            {
                _logger.LogInformation(DeploymentRestartFailedMessage);
            }
        }
        catch (KubernetesObjectNotFoundException)
        {
            await RestartStatefulSet(nameSpace, podNameStarts, cancellationToken);
        }
    }

    private async Task RestartStatefulSet(string nameSpace, string podNameStarts, CancellationToken cancellationToken)
    {
        var statefulSet = await _kubernetesOperations.GetStateFulSetFromPod(nameSpace, podNameStarts, cancellationToken);
        var statefulSetResult = await _kubernetesOperations.RestartStateFulSet(nameSpace, statefulSet, cancellationToken);

        if (statefulSetResult)
        {
            _logger.LogInformation("StatefulSet restarted");
        }
        else
        {
            _logger.LogInformation(StatefulSetRestartFailedMessage);
        }
    }
}
