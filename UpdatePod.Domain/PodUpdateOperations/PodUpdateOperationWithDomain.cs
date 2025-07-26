using Microsoft.Extensions.Logging;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.KubernetesUtils;

namespace UpdatePod.Domain.PodUpdateOperations;

public class PodUpdateOperationWithDomain: IPodUpdateOperations
{
    private readonly IKubernetesOperations _kubernetesOperations;
    private readonly IImageOperations _imageOperations;
    private readonly ILogger<PodUpdateOperationWithDomain> _logger;

    public PodUpdateOperationWithDomain(IKubernetesOperations kubernetesOperations, IImageOperations imageOperations,
        ILogger<PodUpdateOperationWithDomain> logger)
    {
        _kubernetesOperations = kubernetesOperations;
        _imageOperations = imageOperations;
        _logger = logger;
    }
    
    public async  Task UpdatePodImage(string nameSpace, string podNameStarts, string? containerName,
        CancellationToken cancellationToken = default)
    {
         var imagePullPolicy =await  _kubernetesOperations.GetImagePullPolicy(namespaceInfo:nameSpace,
            containerName: containerName,
            podNameStarts:podNameStarts,ct:cancellationToken);
        _logger.LogInformation("Image pull policy: {imagePullPolicy}", imagePullPolicy);

        if (imagePullPolicy != "Always")
        {
            throw new ApplicationException($"Image pull policy is must be  Always but {imagePullPolicy}");
        }
        
        var podImageHash=await _kubernetesOperations.GetPodContainerImageHash(namespaceInfo:nameSpace,podNameStarts:podNameStarts
            , containerName: containerName,ct:cancellationToken);
        
        var containerImageName= await _kubernetesOperations.GetPodContainerImageInfo(namespaceInfo:nameSpace,podNameStarts:podNameStarts
            , containerName: containerName
            ,ct:cancellationToken); 
        
        var latestImageHashFromRegistry=await _imageOperations.GetLatestHashFromImage(containerImageName, ct:cancellationToken);

        if (latestImageHashFromRegistry is null)
        {
            throw new ApplicationException($"Image {containerName} has not been found in registry.");
        }
        
        _logger.LogInformation($"Pod Image hash: {podImageHash} Latest Image " +
                               $"Hash On registry: {latestImageHashFromRegistry}"
            );

        if (latestImageHashFromRegistry != podImageHash)
        {
            try
            {
                var deployment =
                    await _kubernetesOperations.GetDeploymentFromPod(namespaceInfo: nameSpace,
                        podNameStarts: podNameStarts, ct: cancellationToken);
                
                var deploymentResult=await _kubernetesOperations.RestartDeployment(namespaceInfo:nameSpace,deployment,ct:cancellationToken);

                if (deploymentResult)
                {
                    _logger.LogInformation("Deployment restarted");
                }

                else
                {
                    _logger.LogInformation("Deployment restart failed");
                }
            }

            catch (KubernetesObjectNotFoundException)
            { 
                var statefulSet = await _kubernetesOperations.GetStateFulSetFromPod(namespaceInfo: nameSpace,
                   podNameStarts: podNameStarts, ct: cancellationToken);
        
                var statefulSetResult=  await _kubernetesOperations.RestartStateFulSet(namespaceInfo:nameSpace,
                    deployment:statefulSet,ct:cancellationToken);

                 if (statefulSetResult)
                 {
                  _logger.LogInformation("StatefulSet restarted");
                 }

                 else
                 {
                     _logger.LogInformation("StatefulSet restart failed");
                 }
              
            }
        }
        
    }
    
}
