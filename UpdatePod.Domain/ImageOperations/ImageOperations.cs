using System.Net;

namespace UpdatePod.Domain.ImageOperations;

public class ImageOperations: IImageOperations
{
    private readonly ImageOperationsWithDockerIo _imageOperationsWithDockerIo;

    public ImageOperations (HttpClient httpClient)
    {
        _imageOperationsWithDockerIo = new ImageOperationsWithDockerIo(httpClient);
    }
    
    public Task<string?> GetLatestHashFromImage(string image, CancellationToken ct = default)
    {
         var imageInfo=GetHashFromDockerIo(image);
         return _imageOperationsWithDockerIo.GetLatestHash(imageInfo.repository, imageInfo.tag, ct);
        //throw new NotSupportedException("image type is not supported");
    }

    private (string repository, string tag) GetHashFromDockerIo(string image, CancellationToken ct = default)
    {
        var parts = image.Split(':');
        var repository = parts[0].Replace("docker.io/", "");
        var tag = parts.Length > 1 ? parts[1] : "latest";
        return (repository, tag);
    }
}