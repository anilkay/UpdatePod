using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UpdatePod.Domain.ImageOperations.Models;

namespace UpdatePod.Domain.ImageOperations;

public class ImageOperations : IImageOperations
{
    private readonly IDictionary<string, IImageOperationsStrategy> _registryOperations;
    private readonly ImageOperationData _imageOperationData;
    private readonly  IImageOperationsStrategy _imageOperationsStrategyForHarbor;
    

    public ImageOperations(HttpClient httpClient, ImageOperationData imageOperationData)
    {
        _registryOperations = new Dictionary<string, IImageOperationsStrategy>(StringComparer.OrdinalIgnoreCase)
        {
            ["docker.io"] = new ImageOperationsWithDockerIo(httpClient, imageOperationData),
        };
        _imageOperationData = imageOperationData;
        _imageOperationsStrategyForHarbor = new ImageOperationsWithHarbor(httpClient, imageOperationData);
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
        
        var isQuayIoImage=image.Contains("quay.io", StringComparison.OrdinalIgnoreCase);
        
        if ( isQuayIoImage || _imageOperationData.IsHarborUsed() )
        {
          var imageInfo=ParseImageForHarbor(image);
          return _imageOperationsStrategyForHarbor.GetLatestHash(imageInfo.repository, imageInfo.tag,ct);
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

    private static (string repository, string tag)  ParseImageForHarbor(string image)
    {
        var parts = image.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var last = parts[^1];
        var lastPartSplitted=last.Split(":", StringSplitOptions.RemoveEmptyEntries);
        var tag= lastPartSplitted[^1];
        return (image, tag);
    }
    
}
