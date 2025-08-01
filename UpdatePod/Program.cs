using UpdatePod;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.KubernetesUtils;
using UpdatePod.Domain.PodUpdateOperations;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IKubernetesOperations, KubernetesOperationWithKubernetesClient>();

builder.Services.AddSingleton<ImageOperationData>(provider =>
{
    bool? useHarbor = bool.TryParse(Environment.GetEnvironmentVariable("USE_HARBOR"), out var useHarborResult) ? useHarborResult : null;
    string? harborRobotUser = Environment.GetEnvironmentVariable("HARBOR_ROBOT_USER");
    string? harborRobotToken = Environment.GetEnvironmentVariable("HARBOR_ROBOT_TOKEN");
    
    string? imageOperationsTimeoutStr= Environment.GetEnvironmentVariable("IMAGE_OPERATIONS_TIMEOUT");
    bool isImageOperationTimeoutParsed=int.TryParse(imageOperationsTimeoutStr, out var resultImageOperationTimeout);

    return new ImageOperationData()
    {
        UseHarbor = useHarbor,
        HarborRobotUser = harborRobotUser,
        HarborRobotToken = harborRobotToken,
        ImageOperationsTimeout = isImageOperationTimeoutParsed ? resultImageOperationTimeout : null,
    };
});

builder.Services.AddSingleton<IImageOperations, ImageOperations>();
builder.Services.AddSingleton<IPodUpdateOperations, PodUpdateOperationWithDomain>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();