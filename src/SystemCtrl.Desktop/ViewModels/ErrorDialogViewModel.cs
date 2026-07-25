using System;
using System.Threading.Tasks;
using ReactiveUI.SourceGenerators;

namespace SystemCtrl.Desktop.ViewModels;

public partial class ErrorDialogViewModel : ViewModelBase
{
    public ErrorDialogViewModel()
    {
        Title = string.Empty;
        Message = string.Empty;
        Detail = string.Empty;
        CopyButtonText = "Copy to Clipboard";
        IsCopyIconVisible = true;
    }

    public void Init(string title, string message, string detail, Action closeAction, Func<string, Task> copyAction)
    {
        Title = title;
        Message = message;
        Detail = detail;
        _closeAction = closeAction;
        _copyAction = copyAction;
    }
    
    [Reactive]
    public partial string Title { get; set; }

    [Reactive]
    public partial string Message { get; set; }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    [Reactive]
    public partial string Detail { get; set; }

    [Reactive]
    public partial string CopyButtonText { get; set; }

    [Reactive]
    public partial bool IsCopyIconVisible { get; set; }

    private Action? _closeAction;
    private Func<string, Task>? _copyAction;

    [ReactiveCommand]
    public void Close()
    {
        _closeAction?.Invoke();
    }

    [ReactiveCommand]
    public async Task Copy()
    {
        if (_copyAction != null)
        {
            await _copyAction(Detail);
            CopyButtonText = "Copied!";
            IsCopyIconVisible = false;
            
            await Task.Delay(2000);
            
            CopyButtonText = "Copy to Clipboard";
            IsCopyIconVisible = true;
        }
    }
}
