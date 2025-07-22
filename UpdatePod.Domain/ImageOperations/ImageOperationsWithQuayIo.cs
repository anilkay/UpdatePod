using System.Net.Http.Headers;

namespace UpdatePod.Domain.ImageOperations;

public class ImageOperationsWithQuayIo(HttpClient client): IImageOperationsStrategy
{
    public async  Task<string?> GetLatestHash(string repository, string tag, CancellationToken token)
    {
        var url= GetUrl(repository, tag);
        
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.docker.distribution.manifest.v2+json"));
        //Hmm, This tags looks universal However responses not :(

        var response = await client.GetAsync(url,token);
        
        response.EnsureSuccessStatusCode();

        var responseAsStr=await response.Content.ReadAsStringAsync();
        
        var canGetHeader=response.Headers.TryGetValues("docker-content-digest", out var digest);

        return !canGetHeader ? null : digest?.ToList().FirstOrDefault();
    }
    
    private string GetUrl(string imageName, string tag)
    {
        var parts = imageName.Split('/');

        if (parts.Length != 2)
            throw new ArgumentException("Quay image names must be in 'namespace/repository' format.", nameof(imageName));

        var namespacePart = parts[0];
        var repository = parts[1];

        return $"https://quay.io/v2/{namespacePart}/{repository}/manifests/{tag}";
    }
    
}