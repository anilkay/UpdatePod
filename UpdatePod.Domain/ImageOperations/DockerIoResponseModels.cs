namespace UpdatePod.Domain.ImageOperations;

public class DockerIoImageResponseModels
{
    public int? creator { get; set; }
    public int? id { get; set; }
    public Images[]? images { get; set; }
    public string? last_updated { get; set; }
    public int? last_updater { get; set; }
    public string? last_updater_username { get; set; }
    public string? name { get; set; }
    public int? repository { get; set; }
    public int? full_size { get; set; }
    public bool? v2 { get; set; }
    public string? tag_status { get; set; }
    public string? tag_last_pulled { get; set; }
    public string? tag_last_pushed { get; set; }
    public string? media_type { get; set; }
    public string? content_type { get; set; }
    public string? digest { get; set; }
}

public class Images
{
    public string? architecture { get; set; }
    public string? features { get; set; }
    public string? variant { get; set; }
    public string? digest { get; set; }
    public string? os { get; set; }
    public string? os_features { get; set; }
    public int? size { get; set; }
    public string? status { get; set; }
    public string? last_pulled { get; set; }
    public string? last_pushed { get; set; }
}

