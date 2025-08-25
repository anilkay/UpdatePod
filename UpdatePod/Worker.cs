using UpdatePod.Domain.PodUpdateOperations;

namespace UpdatePod;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IPodUpdateOperations _podUpdateOperations;

    public Worker(ILogger<Worker> logger, IPodUpdateOperations podUpdateOperations)
    {
        _logger = logger;
        _podUpdateOperations = podUpdateOperations;
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

            await _podUpdateOperations.UpdatePodImage(workInfo.nameSpace, workInfo.podNameStarts, workInfo.containerName, cancellationToken: stoppingToken);
            
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

}