using System.Collections.Generic;
using System.Threading.Tasks;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IServiceAnalyzer
{
    /// <summary>
    /// Analyzes the given Windows service details and returns a structured QnA format.
    /// </summary>
    /// <param name="serviceInfo">The detailed windows service info to analyze.</param>
    /// <returns>A list of QnA objects containing the analysis.</returns>
    Task<List<AiQna>> AnalyzeServiceAsync(DetailedWindowsServiceInfo serviceInfo);
}
