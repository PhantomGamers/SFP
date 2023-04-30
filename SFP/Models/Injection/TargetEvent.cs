#region

using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global

#endregion

namespace SFP.Models.Injection;

[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum EventType
{
    [JsonPropertyName("Target.targetCreated")] TARGET_CREATED,
    [JsonPropertyName("Target.targetDestroyed")] TARGET_DESTROYED,
    [JsonPropertyName("Target.targetInfoChanged")] TARGET_INFO_CHANGED
}

public record TargetEvent(
    [property: JsonPropertyName("method")] EventType Method,
    [property: JsonPropertyName("params")] Params Params
);

public record Params(
    [property: JsonPropertyName("targetInfo")]
    TargetInfo TargetInfo
);

public record TargetInfo(
    [property: JsonPropertyName("targetId")]
    string TargetId,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("attached")]
    bool Attached,
    [property: JsonPropertyName("browserContextId")]
    string BrowserContextId
);
