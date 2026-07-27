using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SystemCtrl.Core.Models;

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent>? Contents { get; set; }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GeminiRequest))]
[JsonSerializable(typeof(List<AiQna>))]
internal partial class AiJsonContext : JsonSerializerContext
{
}
