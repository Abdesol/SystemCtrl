using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IServiceAnalyzer
{
    Task<List<AiQna>> AnalyzeServiceAsync(DetailedWindowsServiceInfo serviceInfo, AppSettings settings);
}
