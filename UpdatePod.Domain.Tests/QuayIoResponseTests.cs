using Microsoft.Extensions.DependencyInjection;
using Polly;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.ImageOperations.Models;

namespace UpdatePod.Domain.Tests;

public class QuayIoResponseTests
{
    [Fact]
    public async Task QuayIoResponse_ShouldReturnDigest()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        
            services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.Delay = TimeSpan.FromSeconds(5);
                options.Retry.UseJitter = true;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
        

            });
    
        });
            
        await using var provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<HttpClient>();
        ImageOperationData imageOperationData = new();
        
        IImageOperations imageOperations = new ImageOperations.ImageOperations(client, imageOperationData);
        var digest= await imageOperations.GetLatestHashFromImage("quay.io/fedora/httpd-24:latest");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
    }
}