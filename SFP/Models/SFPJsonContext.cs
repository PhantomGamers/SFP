#region

using System.Text.Json.Serialization;
using SFP.Models.Injection.Config;

#endregion

namespace SFP.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(PatchEntry[]))]
[JsonSerializable(typeof(SfpConfig))]
[JsonSerializable(typeof(MillenniumConfig))]
[JsonSerializable(typeof(Patch[]))]
internal partial class SFPJsonContext : JsonSerializerContext;
