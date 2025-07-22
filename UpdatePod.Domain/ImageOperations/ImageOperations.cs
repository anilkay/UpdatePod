using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UpdatePod.Domain.ImageOperations;

public class ImageOperations : IImageOperations
{
    private readonly IDictionary<string, IImageOperationsStrategy> _registryOperations;

    public ImageOperations(HttpClient httpClient)
    {
        _registryOperations = new Dictionary<string, IImageOperationsStrategy>(StringComparer.OrdinalIgnoreCase)
        {
            ["docker.io"] = new ImageOperationsWithDockerIo(httpClient),
            ["quay.io"] = new ImageOperationsWithQuayIo(httpClient)
        };
    }

    public Task<string?> GetLatestHashFromImage(string image, CancellationToken ct = default)
    {
        foreach (var registry in _registryOperations.Keys)
        {
            if (image.Contains(registry, StringComparison.OrdinalIgnoreCase))
            {
                var imageInfo = ParseImage(image, registry);
                return _registryOperations[registry].GetLatestHash(imageInfo.repository, imageInfo.tag, ct);
            }
        }

        throw new NotSupportedException($"Registry not supported for image: {image}");
    }

    private static (string repository, string tag) ParseImage(string image, string registry)
    {
        var parts = image.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var repository = parts[0].Replace($"{registry}/", string.Empty, StringComparison.OrdinalIgnoreCase);
        var tag = parts.Length > 1 ? parts[1] : "latest";
        return (repository, tag);
    }
}
