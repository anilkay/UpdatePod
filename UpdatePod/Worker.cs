using UpdatePod.ImageOperations;
using UpdatePod.KubernetesUtils;

namespace UpdatePod;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IKubernetesOperations _kubernetesOperations;
    private readonly IImageOperations _imageOperations;

    public Worker(ILogger<Worker> logger, IKubernetesOperations kubernetesOperations, IImageOperations imageOperations)
    {
        _logger = logger;
        _kubernetesOperations = kubernetesOperations;
        _imageOperations = imageOperations;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workInfo=GetInfoFromEnvironment();
            
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await UpdatePodImage(workInfo.nameSpace, workInfo.podNameStarts, workInfo.containerName);
            
            await Task.Delay(TimeSpan.FromMinutes(workInfo.intervalMinutes), stoppingToken);
        }
    }

    private (string nameSpace, string podNameStarts, int intervalMinutes, string? containerName) GetInfoFromEnvironment()
    {
        var namespaceInfo = Environment.GetEnvironmentVariable("POD_NAMESPACE") 
                            ?? throw new Exception("POD_NAMESPACE env var is missing.");

        var podNameStarts = Environment.GetEnvironmentVariable("POD_NAME_PREFIX") 
                            ?? throw new Exception("POD_NAME_PREFIX env var is missing.");

        var containerName = Environment.GetEnvironmentVariable("POD_CONTAINER_NAME"); 
        
        var intervalMinutesStr = Environment.GetEnvironmentVariable("RESTART_INTERVAL_MINUTES");
        var intervalMinutes = int.TryParse(intervalMinutesStr, out var result) ? result : 10;
        
        return (namespaceInfo, podNameStarts, intervalMinutes, containerName);
        
    }

    private async Task UpdatePodImage(string nameSpace, string podNameStarts, string? containerName)
    {
        var imagePullPolicy =await  _kubernetesOperations.GetImagePullPolicy(namespaceInfo:nameSpace,podNameStarts:podNameStarts);
        _logger.LogInformation("Image pull policy: {imagePullPolicy}", imagePullPolicy);

        if (imagePullPolicy != "Always")
        {
            throw new ApplicationException($"Image pull policy is must be  Always but {imagePullPolicy}");
        }
        
        var podImageHash=await _kubernetesOperations.GetPodContainerImageHash(namespaceInfo:nameSpace,podNameStarts:podNameStarts);
        
        var containerImageName= await _kubernetesOperations.GetPodContainerImageInfo(namespaceInfo:nameSpace,podNameStarts:podNameStarts); 
        
        var latestImageHashFromRegistry=await _imageOperations.GetLatestHashFromImage(containerImageName);
        
        _logger.LogInformation($"Pod Image hash: {podImageHash} Latest Image " +
                               $"Hash On registry: {latestImageHashFromRegistry}"
            );

        if (latestImageHashFromRegistry != podImageHash)
        {
            var deployment =
                await _kubernetesOperations.GetDeploymentFromPod(namespaceInfo: nameSpace,
                    podNameStarts: podNameStarts);
            
            var deploymentResult=await _kubernetesOperations.RestartDeployment(namespaceInfo:nameSpace,deployment);

            if (deploymentResult)
            {
                _logger.LogInformation("Deployment restarted");
            }

            else
            {
                _logger.LogInformation("Deployment restart failed");
            }
            
        }
        
    }
}