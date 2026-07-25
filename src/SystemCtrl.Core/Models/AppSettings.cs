namespace SystemCtrl.Core.Models;

public class AppSettings
{
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "Gemini 3.5 Flash";
    public List<string> PinnedServices { get; set; } = new();
    public Dictionary<string, List<AiQna>> AiSummaries { get; set; } = new();
}
