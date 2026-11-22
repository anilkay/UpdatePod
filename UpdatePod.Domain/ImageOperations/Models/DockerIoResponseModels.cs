using System.Text.Json.Serialization;

namespace UpdatePod.Domain.ImageOperations;

public record DockerIoImageResponseModels(
    [property: JsonPropertyName("creator")] int? Creator,
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("images")] Images[]? Images,
    [property: JsonPropertyName("last_updated")] string? LastUpdated,
    [property: JsonPropertyName("last_updater")] int? LastUpdater,
    [property: JsonPropertyName("last_updater_username")] string? LastUpdaterUsername,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("repository")] int? Repository,
    [property: JsonPropertyName("full_size")] int? FullSize,
    [property: JsonPropertyName("v2")] bool? V2,
    [property: JsonPropertyName("tag_status")] string? TagStatus,
    [property: JsonPropertyName("tag_last_pulled")] string? TagLastPulled,
    [property: JsonPropertyName("tag_last_pushed")] string? TagLastPushed,
    [property: JsonPropertyName("media_type")] string? MediaType,
    [property: JsonPropertyName("content_type")] string? ContentType,
    [property: JsonPropertyName("digest")] string? Digest
);

public record Images(
    [property: JsonPropertyName("architecture")] string? Architecture,
    [property: JsonPropertyName("features")] string? Features,
    [property: JsonPropertyName("variant")] string? Variant,
    [property: JsonPropertyName("digest")] string? Digest,
    [property: JsonPropertyName("os")] string? Os,
    [property: JsonPropertyName("os_features")] string? OsFeatures,
    [property: JsonPropertyName("size")] int? Size,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("last_pulled")] string? LastPulled,
    [property: JsonPropertyName("last_pushed")] string? LastPushed
);
