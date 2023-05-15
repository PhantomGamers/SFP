#region

using System.Text.Json.Serialization;

#endregion

namespace SFP.Models.Injection.Config;

public class Patch
{
    [JsonPropertyName("url")] public string Url { get; init; } = string.Empty;

    [JsonPropertyName("css")] public string Css { get; init; } = string.Empty;

    [JsonPropertyName("patch-every-instance")]
    public bool PatchEveryInstance { get; init; } = false;

    [JsonPropertyName("remote")] public bool Remote { get; init; } = true;

    [JsonPropertyName("js")] public string Js { get; init; } = string.Empty;
}

public class MillenniumConfig
{
    [JsonPropertyName("patch")] public IEnumerable<Patch> Patch { get; init; } = Array.Empty<Patch>();
}
