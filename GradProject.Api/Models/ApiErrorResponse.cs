using System.Text.Json.Serialization;

namespace GradProject.Api.Models;

/// <summary>
/// Standard JSON body for API errors (middleware and model validation).
/// </summary>
public sealed class ApiErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; init; } = "";

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
