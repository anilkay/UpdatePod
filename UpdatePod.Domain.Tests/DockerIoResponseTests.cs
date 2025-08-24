using Microsoft.Extensions.DependencyInjection;
using Polly;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.ImageOperations.Models;

namespace UpdatePod.Domain.Tests;

public class DockerIoResponseTests
{
    private static  ServiceProvider? GetServiceProvider()
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
        
        return services.BuildServiceProvider();
    }
    [Fact]
    public async Task DockerIoResponse_ShouldReturnDigest()
    {
        await using var provider = GetServiceProvider();
        HttpClient client = provider!.GetRequiredService<HttpClient>();
        ImageOperationData imageOperationData = new();

        IImageOperations imageOperations = new ImageOperations.ImageOperations(client, imageOperationData);
        var digest= await imageOperations.GetLatestHashFromImage("docker.io/httpd:2.4:latest");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
        
    }

    [Fact]
    public async Task DockerIoResponse_ShouldReturnDigestFromUserImamgeWithHash()
    {
        
       
            
        await using var provider = GetServiceProvider();
        HttpClient client = provider!.GetRequiredService<HttpClient>();
        ImageOperationData imageOperationData = new();

        IImageOperations imageOperations = new ImageOperations.ImageOperations(client,imageOperationData);
        var digest= await imageOperations.GetLatestHashFromImage("docker.io/aanilkay/updatepod");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
    }

    [Fact]
    public async Task DockerIoResponseWithToken_ShouldReturnDigestFromToken()
    {
        await using var provider = GetServiceProvider();
        HttpClient client = provider!.GetRequiredService<HttpClient>();
        ImageOperationData imageOperationData = new()
        {
            DockerHubToken = "YourLegitJwtToken" // :)
        };

        IImageOperations imageOperations = new ImageOperations.ImageOperations(client,imageOperationData);
        var digest= await imageOperations.GetLatestHashFromImage("docker.io/aanilkay/updatepod");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
    }

    [Fact]
    public async Task DockerIoResponseWithToken_ShouldReturnNotFound()
    {
        await using var provider = GetServiceProvider();
        HttpClient client = provider!.GetRequiredService<HttpClient>();
        
        ImageOperationData imageOperationData = new()
        {
            DockerHubToken = "sadasdasdasdasd"
        };
        
        await Assert.ThrowsAsync<HttpRequestException>( async () =>
        {
            IImageOperations imageOperations = new ImageOperations.ImageOperations(client,imageOperationData);
            var digest= await imageOperations.GetLatestHashFromImage("docker.io/aanilkay/updatepod");
        });
    }
    
}