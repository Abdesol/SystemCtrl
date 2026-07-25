using System.ServiceProcess;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Core.Models;

public partial class WindowsServiceInfo : ReactiveObject
{
    [Reactive] public partial string DisplayName { get; set; } = string.Empty;
    
    [Reactive] public partial string ServiceName { get; set; } = string.Empty;
    
    [Reactive] public partial string Description { get; set; } = string.Empty;

    [Reactive] public partial ServiceControllerStatus Status { get; set; } 

    [Reactive] public partial ServiceStartMode StartType { get; set; }
    
    [Reactive] public partial DetailedWindowsServiceInfo? DetailedInfo { get; set; }
}