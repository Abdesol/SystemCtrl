using System.Collections.Generic;
using System.Threading.Tasks;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IAiAnalyzer
{
    Task<List<AiQna>> AnalyzeServiceAsync(DetailedWindowsServiceInfo serviceInfo, AppSettings settings);
    Task<List<AiQna>> AnalyzeTaskAsync(WindowsTaskInfo task, DetailedWindowsTaskInfo detailedInfo, AppSettings settings);
}
