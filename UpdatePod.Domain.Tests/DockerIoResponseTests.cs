using UpdatePod.Domain.ImageOperations;

namespace UpdatePod.Domain.Tests;

public class DockerIoResponseTests
{
    [Fact]
    public async Task DockerIoResponse_ShouldReturnDigest()
    {
        HttpClient client = new();
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
        HttpClient client = new();
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
        HttpClient client = new();
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
        HttpClient client = new();
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