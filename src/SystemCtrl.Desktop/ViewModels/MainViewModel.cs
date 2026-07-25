using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using System.Reactive.Linq;
using System.Linq;
using System.Collections.Generic;
using SystemCtrl.Desktop.Services;

namespace SystemCtrl.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowsServiceManager _windowsServiceManager;
    private readonly IWindowsTaskManager _windowsTaskManager;
    private readonly IErrorDialogService _errorDialog;
    private List<WindowsServiceInfo> _allServices;
    private List<WindowsTaskInfo> _allTasks;

    public MainViewModel(
        IServiceProvider serviceProvider,
        IWindowsServiceManager windowsServiceManager,
        IWindowsTaskManager windowsTaskManager,
        IErrorDialogService errorDialog)
    {
        _serviceProvider = serviceProvider;
        _windowsServiceManager = windowsServiceManager;
        _windowsTaskManager = windowsTaskManager;
        _errorDialog = errorDialog;

        _allServices = _windowsServiceManager.GetServices().ToList();
        _allTasks = _windowsTaskManager.GetTasks().ToList();

        SlidePanel = new SlidePanelViewModel();
        ActiveTab = 0;

        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => FilterCurrent());

        this.WhenAnyValue(x => x.ActiveTab)
            .Subscribe(_ =>
            {
                SearchText = string.Empty;
                this.RaisePropertyChanged(nameof(SearchPlaceholder));
                FilterCurrent();
            });

        this.WhenAnyValue(x => x.SlidePanel.IsOpen)
            .Where(isOpen => !isOpen)
            .Subscribe(_ => RefreshCurrent());
    }

    [Reactive] public partial ObservableCollection<ServiceItemViewModel> Services { get; set; }

    [Reactive] public partial ObservableCollection<TaskItemViewModel> Tasks { get; set; }

    [Reactive] public partial string SearchText { get; set; } = string.Empty;

    [Reactive] public partial int ActiveTab { get; set; }

    [Reactive] public partial SlidePanelViewModel SlidePanel { get; set; }

    public string SearchPlaceholder => ActiveTab == 0 ? "Search services..." : "Search tasks...";

    private void FilterCurrent()
    {
        if (ActiveTab == 0)
            FilterServices();
        else
            FilterTasks();
    }

    [ReactiveCommand]
    public void SetActiveTab(string tab)
    {
        if (int.TryParse(tab, out var index))
            ActiveTab = index;
        this.RaisePropertyChanged(nameof(SearchPlaceholder));
    }

    [ReactiveCommand]
    public void Refresh()
    {
        RefreshCurrent();
    }

    private void RefreshCurrent()
    {
        if (ActiveTab == 0)
            RefreshServices();
        else
            RefreshTasks();
    }

    private void FilterServices()
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedServices = settings.PinnedServices;

        IEnumerable<WindowsServiceInfo> filtered = _allServices;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.ToLowerInvariant();
            filtered = _allServices.Where(s =>
                s.DisplayName.ToLowerInvariant().Contains(query) ||
                s.ServiceName.ToLowerInvariant().Contains(query));
        }

        var sorted = filtered
            .OrderByDescending(s => pinnedServices.Contains(s.ServiceName))
            .ThenBy(s => s.DisplayName);

        var viewModels = sorted.Select(s => new ServiceItemViewModel(
            s,
            pinnedServices.Contains(s.ServiceName),
            TogglePinAction,
            OpenServiceAction,
            StartServiceAction,
            StopServiceAction));

        Services = new ObservableCollection<ServiceItemViewModel>(viewModels);
    }

    private void FilterTasks()
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedTasks = settings.PinnedTasks;

        IEnumerable<WindowsTaskInfo> filtered = _allTasks;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.ToLowerInvariant();
            filtered = _allTasks.Where(t =>
                t.TaskName.ToLowerInvariant().Contains(query) ||
                t.TaskPath.ToLowerInvariant().Contains(query));
        }

        var sorted = filtered
            .OrderByDescending(t => pinnedTasks.Contains(t.TaskPath))
            .ThenBy(t => t.TaskName);

        var viewModels = sorted.Select(t => new TaskItemViewModel(
            t,
            pinnedTasks.Contains(t.TaskPath),
            TogglePinTaskAction,
            OpenTaskAction,
            StartTaskAction,
            StopTaskAction));

        Tasks = new ObservableCollection<TaskItemViewModel>(viewModels);
    }

    private void TogglePinAction(ServiceItemViewModel item)
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();

        if (settings.PinnedServices == null!)
            settings.PinnedServices = [];

        if (settings.PinnedServices.Contains(item.Service.ServiceName))
            settings.PinnedServices.Remove(item.Service.ServiceName);
        else
            settings.PinnedServices.Add(item.Service.ServiceName);

        settingsService.SaveSettings(settings);
        FilterServices();
    }

    private void TogglePinTaskAction(TaskItemViewModel item)
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();

        if (settings.PinnedTasks == null!)
            settings.PinnedTasks = [];

        if (settings.PinnedTasks.Contains(item.Task.TaskPath))
            settings.PinnedTasks.Remove(item.Task.TaskPath);
        else
            settings.PinnedTasks.Add(item.Task.TaskPath);

        settingsService.SaveSettings(settings);
        FilterTasks();
    }

    private void OpenServiceAction(ServiceItemViewModel item)
    {
        var serviceDetailViewModel = _serviceProvider.GetRequiredService<ServiceDetailViewModel>();
        serviceDetailViewModel.Load(item.Service);
        SlidePanel.Open(item.Service.DisplayName, serviceDetailViewModel);
    }

    private void OpenTaskAction(TaskItemViewModel item)
    {
        var taskDetailViewModel = _serviceProvider.GetRequiredService<TaskDetailViewModel>();
        taskDetailViewModel.Load(item.Task);
        SlidePanel.Open(item.Task.TaskName, taskDetailViewModel);
    }

    private async void StartServiceAction(ServiceItemViewModel item)
    {
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsServiceManager.Start(item.Service.ServiceName));
            await System.Threading.Tasks.Task.Delay(1000);
            RefreshServices();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to start '{item.DisplayName}'", ex.Message, ex.ToString());
        }
    }

    private async void StopServiceAction(ServiceItemViewModel item)
    {
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsServiceManager.Stop(item.Service.ServiceName));
            await System.Threading.Tasks.Task.Delay(1000);
            RefreshServices();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to stop '{item.DisplayName}'", ex.Message, ex.ToString());
        }
    }

    private async void StartTaskAction(TaskItemViewModel item)
    {
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsTaskManager.Run(item.Task.TaskPath));
            await System.Threading.Tasks.Task.Delay(1000);
            RefreshTasks();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to start '{item.DisplayName}'", ex.Message, ex.ToString());
        }
    }

    private async void StopTaskAction(TaskItemViewModel item)
    {
        try
        {
            await System.Threading.Tasks.Task.Run(() => _windowsTaskManager.Stop(item.Task.TaskPath));
            await System.Threading.Tasks.Task.Delay(1000);
            RefreshTasks();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to stop '{item.DisplayName}'", ex.Message, ex.ToString());
        }
    }

    [ReactiveCommand]
    public void OpenSettings()
    {
        var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
        settingsViewModel.OnSaved = () => SlidePanel.Close();
        SlidePanel.Open("Settings", settingsViewModel);
    }

    [ReactiveCommand]
    public void RefreshServices()
    {
        _allServices = _windowsServiceManager.GetServices().ToList();
        FilterServices();
    }

    [ReactiveCommand]
    public void RefreshTasks()
    {
        _allTasks = _windowsTaskManager.GetTasks().ToList();
        FilterTasks();
    }
}