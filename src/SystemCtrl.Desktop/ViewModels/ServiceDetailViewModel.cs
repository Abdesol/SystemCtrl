using SystemCtrl.Core.Models;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Desktop.ViewModels;

public partial class ServiceDetailViewModel : ViewModelBase
{
    [Reactive]
    public partial WindowsServiceInfo? Service { get; set; }

    public void Load(WindowsServiceInfo service)
    {
        Service = service;
    }
}
