namespace UpdatePod.Domain.ImageOperations;

public record DockerIoImageResponseModels(
    int? creator,
    int? id,
    Images[]? images,
    string? last_updated,
    int? last_updater,
    string? last_updater_username,
    string? name,
    int? repository,
    int? full_size,
    bool? v2,
    string? tag_status,
    string? tag_last_pulled,
    string? tag_last_pushed,
    string? media_type,
    string? content_type,
    string? digest
);

public record Images(
    string? architecture,
    string? features,
    string? variant,
    string? digest,
    string? os,
    string? os_features,
    int? size,
    string? status,
    string? last_pulled,
    string? last_pushed
);
