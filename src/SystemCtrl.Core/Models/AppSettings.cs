namespace SystemCtrl.Core.Models;

public class AppSettings
{
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "Gemini 3.5 Flash";
    public System.Collections.Generic.List<string> PinnedServices { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, string> AiSummaries { get; set; } = new();
}
