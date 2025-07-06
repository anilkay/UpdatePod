using System.Text.Json;

namespace UpdatePod.ImageOperations;

public class ImageOperationsWithDockerIo: IImageOperations
{
    private readonly HttpClient _client;

    public ImageOperationsWithDockerIo(HttpClient httpClient)
    {
        _client = httpClient;
    }
    public async Task<string> GetLatestHashFromImage(string image)
    { 
        var parts = image.Split(':');
        var repository = parts[0].Replace("docker.io/", "");
        var tag = parts.Length > 1 ? parts[1] : "latest";
        return await   GetImageDigest(repository, tag);
    }
    
    private  async Task<string> GetImageDigest(string repository, string tag)
    {
        _client.DefaultRequestHeaders.Add("Accept", "application/json");

        var url = $"https://hub.docker.com/v2/repositories/library/{repository}/tags/{tag}";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);

        var digest = doc.RootElement
            .GetProperty("images")[0]
            .GetProperty("digest").GetString();

        return digest;
    }
}