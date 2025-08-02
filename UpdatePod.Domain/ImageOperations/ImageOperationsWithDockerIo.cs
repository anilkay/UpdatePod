using System.Text.Json;
using UpdatePod.Domain.Utils;

namespace UpdatePod.Domain.ImageOperations;

public class ImageOperationsWithDockerIo(HttpClient httpClient, ImageOperationData imageOperationData) : IImageOperationsStrategy
{
   
    
    public async Task<string?> GetLatestHash(string repository, string tag, CancellationToken ct = default)
    {
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        
        var cancelletationTokenWithTimeoutCancellation=HttpClientUtils.
            GenerateCancellationTokenWithTimeout(ct, TimeSpan.FromSeconds(imageOperationData.GetImageOperationsTimeout()));
        
        var url = GetUrl(repository, tag);

        var response = await httpClient.GetAsync(url, cancellationToken: cancelletationTokenWithTimeoutCancellation);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken: ct);
        
        //This part still problematic. Still needs research. 
        var responseAsJson = JsonSerializer.Deserialize<DockerIoImageResponseModels>(content);

        return responseAsJson?.digest; 
    }

    private string GetUrl(string imageName, string tag)
    {
        var parts = imageName.Split('/');

        string username, repository;

        if (parts.Length == 2)
        {
            username = parts[0];
            repository = parts[1];
        }
        else
        {
            username = "library";
            repository = parts[0];
        }

        return $"https://hub.docker.com/v2/repositories/{username}/{repository}/tags/{tag}";
    }
    
}