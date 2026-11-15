using UpdatePod.Domain.PodUpdateOperations;

namespace UpdatePod;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IPodUpdateOperations _podUpdateOperations;
    private readonly PodUpdateOptions _options;

    public Worker(ILogger<Worker> logger, IPodUpdateOperations podUpdateOperations, PodUpdateOptions options)
    {
        _logger = logger;
        _podUpdateOperations = podUpdateOperations;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await _podUpdateOperations.UpdatePodImage(
                _options.Namespace,
                _options.PodNamePrefix,
                _options.ContainerName,
                cancellationToken: stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
        }
    }
}
