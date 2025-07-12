using UpdatePod;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.KubernetesUtils;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IKubernetesOperations, KubernetesOperationWithKubernetesClient>();
builder.Services.AddSingleton<IImageOperations, ImageOperationsWithDockerIo>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();