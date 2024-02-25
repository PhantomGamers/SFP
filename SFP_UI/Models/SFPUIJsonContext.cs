#region

using System.Text.Json.Serialization;
using SFP_UI.Pages;

#endregion

namespace SFP_UI.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SkinBrowserPage.SkinInfo[]))]
[JsonSerializable(typeof(SkinBrowserPage.GithubInfo))]
[JsonSerializable(typeof(SkinBrowserPage.DataInfo))]
internal partial class SFPUIJsonContext : JsonSerializerContext;
