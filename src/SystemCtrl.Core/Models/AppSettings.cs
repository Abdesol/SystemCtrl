namespace SystemCtrl.Core.Models;

public class AppSettings
{
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiModel { get; set; } = "Gemini 3.5 Flash";
    public List<string> PinnedServices { get; set; } = new();
    public List<string> PinnedTasks { get; set; } = new();
    public Dictionary<string, List<AiQna>> AiSummaries { get; set; } = new();
    public bool ShowAiSummary { get; set; } = true;
    public string AppTheme { get; set; } = "Light";
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public int? WindowPositionX { get; set; }
    public int? WindowPositionY { get; set; }
    public string WindowState { get; set; } = "Normal";
}
