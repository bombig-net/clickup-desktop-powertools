using System.Text.Json.Serialization;

namespace ClickUpDesktopPowerTools.Tools.TimeTracking;

public class ClickUpTimeEntryDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("task")]
    public ClickUpTaskDto? Task { get; set; }

    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("at")]
    public string? At { get; set; }
}

public class ClickUpTaskDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class ClickUpTimeEntryResponseDto
{
    [JsonPropertyName("data")]
    public ClickUpTimeEntryDto? Data { get; set; }
}

public class ClickUpStartTimeEntryRequestDto
{
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public string[]? Tags { get; set; }

    [JsonPropertyName("start")]
    public long Start { get; set; }

    [JsonPropertyName("billable")]
    public bool? Billable { get; set; }

    [JsonPropertyName("duration")]
    public long? Duration { get; set; }
}

public class ClickUpStartTimeEntryResponseDto
{
    [JsonPropertyName("data")]
    public ClickUpTimeEntryDto? Data { get; set; }
}

