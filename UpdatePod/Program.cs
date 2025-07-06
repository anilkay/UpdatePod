using UpdatePod;
using UpdatePod.ImageOperations;
using UpdatePod.KubernetesUtils;
using Microsoft.Extensions.Http;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IKubernetesOperations, KubernetesOperationWithKubernetesClient>();
builder.Services.AddSingleton<IImageOperations, ImageOperationsWithDockerIo>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();