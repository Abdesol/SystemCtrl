using System;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Desktop.Services;

namespace SystemCtrl.Desktop.ViewModels;

public partial class SlidePanelViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [Reactive]
    public partial bool IsOpen { get; set; }

    [Reactive]
    public partial string Title { get; set; }

    [Reactive]
    public partial ViewModelBase? Content { get; set; }

    [Reactive]
    public partial double PanelWidth { get; set; }

    public SlidePanelViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Title = string.Empty;
        var settings = _settingsService.LoadSettings();
        PanelWidth = settings.SlidePanelWidth > 0 ? settings.SlidePanelWidth : 400;
    }

    public void SaveWidth()
    {
        var s = _settingsService.LoadSettings();
        if (Math.Abs(s.SlidePanelWidth - PanelWidth) > 1)
        {
            s.SlidePanelWidth = PanelWidth;
            _settingsService.SaveSettings(s);
        }
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
