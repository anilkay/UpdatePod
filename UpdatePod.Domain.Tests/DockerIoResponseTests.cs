using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.ImageOperations.Models;

namespace UpdatePod.Domain.Tests;

public class DockerIoResponseTests
{
    private const int ExpectedSha256DigestLength = 71;

    private sealed class UnauthorizedHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }

    private static  ServiceProvider? GetServiceProvider()
    {
        const int maxRetryAttempts = 2;
        const int baseRetryDelaySeconds = 5;

        var services = new ServiceCollection();
        services.AddHttpClient();
        
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = maxRetryAttempts;
                options.Retry.Delay = TimeSpan.FromSeconds(baseRetryDelaySeconds);
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
        Assert.Equal(ExpectedSha256DigestLength,digest.Length);
        Assert.Contains("sha", digest);
        
    }

    [Fact]
    public async Task DockerIoResponse_ShouldReturnDigestFromUserImageWithHash()
    {
        
       
            
        await using var provider = GetServiceProvider();
        HttpClient client = provider!.GetRequiredService<HttpClient>();
        ImageOperationData imageOperationData = new();

        IImageOperations imageOperations = new ImageOperations.ImageOperations(client,imageOperationData);
        var digest= await imageOperations.GetLatestHashFromImage("docker.io/aanilkay/updatepod");

        Assert.NotNull(digest);
        Assert.Equal(ExpectedSha256DigestLength,digest.Length);
        Assert.Contains("sha", digest);
    }

    [Fact]
    public async Task DockerIoResponseWithToken_ShouldSendTokenAndReturnDigest()
    {
        const string digest = "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        using var handler = new StubHandler(HttpStatusCode.OK, $$"""{"digest":"{{digest}}"}""");
        using var client = new HttpClient(handler);

        ImageOperationData imageOperationData = new()
        {
            DockerHubToken = "YourLegitJwtToken"
        };

        IImageOperations imageOperations = new ImageOperations.ImageOperations(client, imageOperationData);

        var result = await imageOperations.GetLatestHashFromImage("docker.io/aanilkay/updatepod");

        Assert.Equal(digest, result);
        Assert.Equal("JWT", handler.AuthorizationScheme);
        Assert.Equal("YourLegitJwtToken", handler.AuthorizationParameter);
    }

    [Fact]
    public async Task DockerIoResponseWithInvalidToken_ShouldThrow()
    {
        using var client = new HttpClient(new UnauthorizedHandler());

        ImageOperationData imageOperationData = new()
        {
            DockerHubToken = "sadasdasdasdasd"
        };

        IImageOperations imageOperations = new ImageOperations.ImageOperations(client, imageOperationData);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            imageOperations.GetLatestHashFromImage("docker.io/aanilkay/updatepod"));
    }
    
}