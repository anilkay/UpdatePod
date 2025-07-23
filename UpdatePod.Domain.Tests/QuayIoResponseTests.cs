using UpdatePod.Domain.ImageOperations;

namespace UpdatePod.Domain.Tests;

public class QuayIoResponseTests
{
    [Fact]
    public async Task QuayIoResponse_ShouldReturnDigest()
    {
        HttpClient client = new();
        ImageOperationData imageOperationData = new();
        
        IImageOperations imageOperations = new ImageOperations.ImageOperations(client, imageOperationData);
        var digest= await imageOperations.GetLatestHashFromImage("quay.io/fedora/httpd-24");

        Assert.NotNull(digest);
        Assert.Equal(71,digest.Length);
        Assert.Contains("sha", digest);
    }
}