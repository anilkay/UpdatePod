using System.Text.Json;

namespace UpdatePod.Domain.ImageOperations;

public class ImageOperationsWithDockerIo(HttpClient httpClient) : IImageOperations
{
    public async Task<string> GetLatestHashFromImage(string image, CancellationToken ct = default)
    { 
        var parts = image.Split(':');
        var repository = parts[0].Replace("docker.io/", "");
        var tag = parts.Length > 1 ? parts[1] : "latest";
        return await   GetImageDigest(repository, tag,ct) ?? string.Empty;
    }
    
    private  async Task<string?> GetImageDigest(string repository, string tag, CancellationToken ct = default)
    {
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var url = $"https://hub.docker.com/v2/repositories/library/{repository}/tags/{tag}";

        var response = await httpClient.GetAsync(url, cancellationToken:ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        //This part is problematic. Needs more research
        var digest = doc.RootElement
            .GetProperty("digest").GetString();
        

        return digest;
    }
}