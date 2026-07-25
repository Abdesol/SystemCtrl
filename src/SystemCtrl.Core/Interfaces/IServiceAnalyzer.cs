using System.Threading.Tasks;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Core.Interfaces;

public interface IServiceAnalyzer
{
    /// <summary>
    /// Analyzes the given Windows service details and returns a summary of what it does.
    /// </summary>
    /// <param name="serviceInfo">The detailed information about the Windows service.</param>
    /// <returns>A string containing the AI-generated summary and analysis.</returns>
    Task<string> AnalyzeServiceAsync(DetailedWindowsServiceInfo serviceInfo);
}
