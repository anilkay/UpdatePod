using UpdatePod;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.KubernetesUtils;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IKubernetesOperations, KubernetesOperationWithKubernetesClient>();

builder.Services.AddSingleton<ImageOperationData>(provider =>
{
    bool? useHarbor = bool.TryParse(Environment.GetEnvironmentVariable("USE_HARBOR"), out var useHarborResult) ? useHarborResult : null;
    string? harborRobotUser = Environment.GetEnvironmentVariable("HARBOR_ROBOT_USER");
    string? harborRobotToken = Environment.GetEnvironmentVariable("HARBOR_ROBOT_TOKEN");

    return new ImageOperationData()
    {
        UseHarbor = useHarbor,
        HarborRobotUser = harborRobotUser,
        HarborRobotToken = harborRobotToken,
    };
});

builder.Services.AddSingleton<IImageOperations, ImageOperations>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();