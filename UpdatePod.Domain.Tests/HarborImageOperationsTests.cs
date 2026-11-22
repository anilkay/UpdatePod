using System.Net;
using UpdatePod.Domain.ImageOperations;
using UpdatePod.Domain.ImageOperations.Models;

namespace UpdatePod.Domain.Tests;

public class HarborImageOperationsTests
{
    private const string Image = "harbor.example.com/project/app:latest";

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private static ImageOperationsWithHarbor CreateHarbor(RecordingHandler handler) =>
        new(new HttpClient(handler), new ImageOperationData(
            UseHarbor: true, HarborRobotUser: "robot$project", HarborRobotToken: "secret"));

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetLatestHash_DoesNotFallBackToHttpOnHttpsFailure(HttpStatusCode status)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(status));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateHarbor(handler).GetLatestHash(Image, "latest", CancellationToken.None));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(Uri.UriSchemeHttps, request.RequestUri!.Scheme);
    }

    [Fact]
    public async Task GetLatestHash_DoesNotFallBackToHttpOnTransportError()
    {
        var handler = new RecordingHandler(_ => throw new HttpRequestException("TLS handshake failed"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateHarbor(handler).GetLatestHash(Image, "latest", CancellationToken.None));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(Uri.UriSchemeHttps, request.RequestUri!.Scheme);
    }

    [Fact]
    public async Task GetLatestHash_ReturnsDigestHeaderOverHttps()
    {
        const string digest = "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add("Docker-Content-Digest", digest);
            return response;
        });

        var result = await CreateHarbor(handler).GetLatestHash(Image, "latest", CancellationToken.None);

        Assert.Equal(digest, result);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://harbor.example.com/v2/project/app/manifests/latest", request.RequestUri!.ToString());
    }
}
