using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Greeting = "Hello World!";
    }
    
    [Reactive] public partial string Greeting { get; set; }
}