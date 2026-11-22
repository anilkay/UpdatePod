using System.Net.Http.Headers;
using System.Text;
using UpdatePod.Domain.ImageOperations.Models;
using UpdatePod.Domain.Utils;

namespace UpdatePod.Domain.ImageOperations;

public class ImageOperationsWithHarbor(HttpClient httpClient, ImageOperationData imageOperationData)
    : IImageOperationsStrategy
{
   
    public async  Task<string?> GetLatestHash(string repository, string tag, CancellationToken token)
    {
        var url=GetUrl(repository, tag);

        httpClient.DefaultRequestHeaders.Accept.Clear();

        using var timeoutCts = HttpClientUtils.CreateTimeoutTokenSource(
            token, TimeSpan.FromSeconds(imageOperationData.GetImageOperationsTimeout()));

        if (imageOperationData.IsHarborUsed())
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{imageOperationData.HarborRobotUser}:{imageOperationData.HarborRobotToken}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.oci.image.index.v1+json"));
        }
        
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.list.v2+json"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));

        try
        {
            return await GetDigestAsync(url, timeoutCts.Token);
        }
        catch (HttpRequestException)
        {
            var httpUrl = url.Replace("https://", "http://");
            return await GetDigestAsync(httpUrl, timeoutCts.Token);
        }
    }

    private async Task<string?> GetDigestAsync(string url, CancellationToken token)
    {
        using var response = await httpClient.GetAsync(url, token);
        response.EnsureSuccessStatusCode();

        return response.Headers.TryGetValues("docker-content-digest", out var digest)
            ? digest?.FirstOrDefault()
            : null;
    }
    
    private static string GetUrl(string imageName, string tag)
    {
        var parts = imageName.Split('/');

       
        var firstPart = parts[0];
        var secondPart = imageName.Replace(firstPart, string.Empty);
        var secondPartCleaned = secondPart
            .Replace(":", string.Empty)
            .Replace(tag, string.Empty);

        return $"https://{firstPart}/v2{secondPartCleaned}/manifests/{tag}";
    }

   
    
}