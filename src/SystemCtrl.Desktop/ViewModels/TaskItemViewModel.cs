using System;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Models;

namespace SystemCtrl.Desktop.ViewModels;

public partial class TaskItemViewModel : ViewModelBase
{
    private readonly Action<TaskItemViewModel> _onTogglePin;
    private readonly Action<TaskItemViewModel> _onOpen;

    public TaskItemViewModel(
        WindowsTaskInfo task,
        bool isPinned,
        Action<TaskItemViewModel> onTogglePin,
        Action<TaskItemViewModel> onOpen,
        Action<TaskItemViewModel> onStart,
        Action<TaskItemViewModel> onStop)
    {
        Task = task;
        IsPinned = isPinned;
        _onTogglePin = onTogglePin;
        _onOpen = onOpen;
    }

    [Reactive] public partial WindowsTaskInfo Task { get; set; }

    [Reactive] public partial bool IsPinned { get; set; }

    [Reactive] public partial bool IsBusy { get; set; }

    public string PinActionText => IsPinned ? "Unpin" : "Pin";

    public string DisplayName => Task.TaskName;
    public string TaskPath => Task.TaskPath;
    public string Description => Task.Description;
    public TaskState Status => Task.Status;
    public string TriggerSummary => Task.TriggerSummary;
    public DateTime? NextRunTime => Task.NextRunTime;

    [ReactiveCommand]
    public void TogglePinCommand()
    {
        IsPinned = !IsPinned;
        this.RaisePropertyChanged(nameof(PinActionText));
        _onTogglePin?.Invoke(this);
    }

    [ReactiveCommand]
    public void OpenTaskCommand()
    {
        _onOpen?.Invoke(this);
    }
}
