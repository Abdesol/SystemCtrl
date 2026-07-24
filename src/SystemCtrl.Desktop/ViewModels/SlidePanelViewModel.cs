using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Desktop.ViewModels;

public partial class SlidePanelViewModel : ViewModelBase
{
    [Reactive]
    public partial bool IsOpen { get; set; }

    [Reactive]
    public partial string Title { get; set; }

    [Reactive]
    public partial ViewModelBase? Content { get; set; }

    public SlidePanelViewModel()
    {
        Title = string.Empty;
    }

    public void Open(string title, ViewModelBase content)
    {
        Title = title;
        Content = content;
        IsOpen = true;
    }

    [ReactiveCommand]
    public void Close()
    {
        IsOpen = false;
    }
}
