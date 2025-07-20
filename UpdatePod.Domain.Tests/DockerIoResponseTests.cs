using UpdatePod.Domain.ImageOperations;

namespace UpdatePod.Domain.Tests;

public class DockerIoResponseTests
{
    [Fact]
    public async Task DockerIoResponse_ShouldReturnDigest()
    {
        HttpClient client = new();
        IImageOperations imageOperations = new ImageOperations.ImageOperations(client);
        var digest= await imageOperations.GetLatestHashFromImage("httpd:2.4");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
        
    }

    [Fact]
    public async Task DockerIoResponse_ShouldReturnDigestFromUserImamgeWithHash()
    {
        HttpClient client = new();
        IImageOperations imageOperations = new ImageOperations.ImageOperations(client);
        var digest= await imageOperations.GetLatestHashFromImage("aanilkay/updatepod");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
    }
}