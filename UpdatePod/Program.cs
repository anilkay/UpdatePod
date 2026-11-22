using Polly;
using UpdatePod;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.ImageOperations.Models;
using UpdatePod.Domain.KubernetesUtils;
using UpdatePod.Domain.PodUpdateOperations;

const int maxHttpRetryAttempts = 2;
const int httpRetryBaseDelaySeconds = 5;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = maxHttpRetryAttempts;
        options.Retry.Delay = TimeSpan.FromSeconds(httpRetryBaseDelaySeconds);
        options.Retry.UseJitter = true;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
    });
});

// Validate configuration before the host starts so bad values fail fast with a clear message.
var podUpdateOptions = PodUpdateOptions.FromEnvironment();
var imageOperationData = ImageOperationData.FromEnvironment();

builder.Services.AddSingleton(podUpdateOptions);
builder.Services.AddSingleton(imageOperationData);

builder.Services.AddSingleton<IKubernetesOperations, KubernetesOperationWithKubernetesClient>();
builder.Services.AddSingleton<IImageOperations, ImageOperations>();
builder.Services.AddSingleton<IPodUpdateOperations, PodUpdateOperationWithDomain>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
