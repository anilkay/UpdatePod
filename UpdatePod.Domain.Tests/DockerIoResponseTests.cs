using UpdatePod.Domain.ImageOperations;

namespace UpdatePod.Domain.Tests;

public class DockerIoResponseTests
{
    [Fact]
    public async Task DockerIoResponse_ShouldReturnDigest()
    {
        HttpClient client = new();
        IImageOperations imageOperations = new ImageOperationsWithDockerIo(client);
        var digest= await imageOperations.GetLatestHashFromImage("httpd:2.4");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
        
    }
}