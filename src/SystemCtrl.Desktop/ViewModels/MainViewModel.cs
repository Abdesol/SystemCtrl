using System;
using System.Collections.ObjectModel;
using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32.TaskScheduler;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SystemCtrl.Core.Interfaces;
using SystemCtrl.Core.Models;
using System.Reactive.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using SystemCtrl.Desktop.Services;
using Task = System.Threading.Tasks.Task;

namespace SystemCtrl.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWindowsServiceManager _windowsServiceManager;
    private readonly IWindowsTaskManager _windowsTaskManager;
    private readonly IErrorDialogService _errorDialog;
    private List<WindowsServiceInfo> _allServices;
    private List<WindowsTaskInfo> _allTasks;
    private List<ServiceItemViewModel> _allServiceViewModels;
    private List<TaskItemViewModel> _allTaskViewModels;

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

        _allServices = [];
        _allTasks = [];
        _allServiceViewModels = [];
        _allTaskViewModels = [];

        SlidePanel = new SlidePanelViewModel();
        ActiveTab = 0;

        this.WhenAnyValue(x => x.SearchText)
            .Subscribe(_ => FilterCurrent());

        this.WhenAnyValue(x => x.ActiveTab)
            .Subscribe(_ =>
            {
                SearchText = string.Empty;
                IsFilterPopupOpen = false;
                this.RaisePropertyChanged(nameof(SearchPlaceholder));
                RaiseFilterChanged();
                FilterCurrent();
            });

        this.WhenAnyValue(x => x.SlidePanel.IsOpen)
            .Where(isOpen => !isOpen)
            .Subscribe(async void (_) => await RefreshCurrent());

        this.WhenAnyValue(
                x => x.FilterServiceRunning,
                x => x.FilterServiceStopped,
                x => x.FilterServiceAutomatic,
                x => x.FilterServiceManual,
                x => x.FilterServiceDisabled)
            .Subscribe(_ =>
            {
                RaiseFilterChanged();
                if (ActiveTab == 0) FilterCurrent();
            });

        this.WhenAnyValue(
                x => x.FilterTaskReady,
                x => x.FilterTaskRunning,
                x => x.FilterTaskDisabled,
                x => x.FilterTaskQueued)
            .Subscribe(_ =>
            {
                RaiseFilterChanged();
                if (ActiveTab == 1) FilterCurrent();
            });

        _ = InitializeAsync()
            .ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    throw t.Exception.Flatten();
                }
            }, TaskScheduler.Default);
    }

    private async Task InitializeAsync()
    {
        var services = _windowsServiceManager.GetServices().ToList();
        var tasks = _windowsTaskManager.GetTasks().ToList();

        _allServices = services;
        _allTasks = tasks;
        
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedServices = settings.PinnedServices ?? [];
        var pinnedTasks = settings.PinnedTasks ?? [];

        _allServiceViewModels = _allServices.Select(s => new ServiceItemViewModel(
            s,
            pinnedServices.Contains(s.ServiceName),
            TogglePinAction,
            OpenServiceAction,
            StartServiceAction,
            StopServiceAction)).ToList();
            
        _allTaskViewModels = _allTasks.Select(t => new TaskItemViewModel(
            t,
            pinnedTasks.Contains(t.TaskPath),
            TogglePinTaskAction,
            OpenTaskAction,
            StartTaskAction,
            StopTaskAction)).ToList();

        await Dispatcher.UIThread.InvokeAsync(FilterCurrent);
    }

    [Reactive] public partial ObservableCollection<ServiceItemViewModel> Services { get; set; }

    [Reactive] public partial ObservableCollection<TaskItemViewModel> Tasks { get; set; }

    [Reactive] public partial string SearchText { get; set; } = string.Empty;

    [Reactive] public partial int ActiveTab { get; set; }

    [Reactive] public partial SlidePanelViewModel SlidePanel { get; set; }

    [Reactive] public partial bool IsFilterPopupOpen { get; set; }

    [Reactive] public partial bool FilterServiceRunning { get; set; }
    [Reactive] public partial bool FilterServiceStopped { get; set; }
    [Reactive] public partial bool FilterServiceAutomatic { get; set; }
    [Reactive] public partial bool FilterServiceManual { get; set; }
    [Reactive] public partial bool FilterServiceDisabled { get; set; }

    [Reactive] public partial bool FilterTaskReady { get; set; }
    [Reactive] public partial bool FilterTaskRunning { get; set; }
    [Reactive] public partial bool FilterTaskDisabled { get; set; }
    [Reactive] public partial bool FilterTaskQueued { get; set; }

    public string SearchPlaceholder => ActiveTab == 0 ? "Search services..." : "Search tasks...";

    public int ActiveFilterCount
    {
        get
        {
            if (ActiveTab == 0)
            {
                int count = 0;
                if (FilterServiceRunning) count++;
                if (FilterServiceStopped) count++;
                if (FilterServiceAutomatic) count++;
                if (FilterServiceManual) count++;
                if (FilterServiceDisabled) count++;
                return count;
            }
            else
            {
                int count = 0;
                if (FilterTaskReady) count++;
                if (FilterTaskRunning) count++;
                if (FilterTaskDisabled) count++;
                if (FilterTaskQueued) count++;
                return count;
            }
        }
    }

    public bool HasActiveFilters => ActiveFilterCount > 0;

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
    public async Task Refresh()
    {
        await RefreshCurrent();
    }

    [ReactiveCommand]
    public void ToggleFilterPopup()
    {
        IsFilterPopupOpen = !IsFilterPopupOpen;
    }

    [ReactiveCommand]
    public void ClearFilters()
    {
        if (ActiveTab == 0)
        {
            FilterServiceRunning = false;
            FilterServiceStopped = false;
            FilterServiceAutomatic = false;
            FilterServiceManual = false;
            FilterServiceDisabled = false;
        }
        else
        {
            FilterTaskReady = false;
            FilterTaskRunning = false;
            FilterTaskDisabled = false;
            FilterTaskQueued = false;
        }
    }

    private void RaiseFilterChanged()
    {
        this.RaisePropertyChanged(nameof(ActiveFilterCount));
        this.RaisePropertyChanged(nameof(HasActiveFilters));
    }

    private async Task RefreshCurrent()
    {
        if (ActiveTab == 0)
            await RefreshServices();
        else
            await RefreshTasks();
    }

    private void FilterServices()
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedServices = settings.PinnedServices ?? [];

        IEnumerable<ServiceItemViewModel> filtered = _allServiceViewModels;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.ToLowerInvariant();
            filtered = filtered.Where(s =>
                s.DisplayName.Contains(query, StringComparison.InvariantCultureIgnoreCase) ||
                s.ServiceName.Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }

        var hasStatusFilter = FilterServiceRunning || FilterServiceStopped;
        if (hasStatusFilter)
        {
            filtered = filtered.Where(s =>
                (FilterServiceRunning && s.Status == ServiceControllerStatus.Running) ||
                (FilterServiceStopped && s.Status == ServiceControllerStatus.Stopped));
        }

        var hasTypeFilter = FilterServiceAutomatic || FilterServiceManual || FilterServiceDisabled;
        if (hasTypeFilter)
        {
            filtered = filtered.Where(s =>
                (FilterServiceAutomatic && s.StartType == ServiceStartMode.Automatic) ||
                (FilterServiceManual && s.StartType == ServiceStartMode.Manual) ||
                (FilterServiceDisabled && s.StartType == ServiceStartMode.Disabled));
        }

        foreach (var vm in _allServiceViewModels)
        {
            vm.IsPinned = pinnedServices.Contains(vm.ServiceName);
        }

        var sorted = filtered
            .OrderByDescending(s => s.IsPinned)
            .ThenBy(s => s.DisplayName);

        Services = new ObservableCollection<ServiceItemViewModel>(sorted);
    }

    private void FilterTasks()
    {
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedTasks = settings.PinnedTasks ?? [];

        IEnumerable<TaskItemViewModel> filtered = _allTaskViewModels;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.ToLowerInvariant();
            filtered = filtered.Where(t =>
                t.Task.TaskName.Contains(query, StringComparison.InvariantCultureIgnoreCase) ||
                t.Task.TaskPath.Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }

        var hasStatusFilter = FilterTaskReady || FilterTaskRunning || FilterTaskDisabled || FilterTaskQueued;
        if (hasStatusFilter)
        {
            filtered = filtered.Where(t =>
                (FilterTaskReady && t.Task.Status == TaskState.Ready) ||
                (FilterTaskRunning && t.Task.Status == TaskState.Running) ||
                (FilterTaskDisabled && t.Task.Status == TaskState.Disabled) ||
                (FilterTaskQueued && t.Task.Status == TaskState.Queued));
        }

        foreach (var vm in _allTaskViewModels)
        {
            vm.IsPinned = pinnedTasks.Contains(vm.Task.TaskPath);
        }

        var sorted = filtered
            .OrderByDescending(t => t.IsPinned)
            .ThenBy(t => t.DisplayName);

        Tasks = new ObservableCollection<TaskItemViewModel>(sorted);
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
        
        item.IsPinned = settings.PinnedServices.Contains(item.Service.ServiceName);
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
        
        item.IsPinned = settings.PinnedTasks.Contains(item.Task.TaskPath);
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
            item.IsBusy = true;
            await Task.Run(() => _windowsServiceManager.Start(item.Service.ServiceName));
            await Task.Delay(1000);
            await RefreshServices();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to start '{item.DisplayName}'", ex.Message, ex.ToString());
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    private async void StopServiceAction(ServiceItemViewModel item)
    {
        try
        {
            item.IsBusy = true;
            await Task.Run(() => _windowsServiceManager.Stop(item.Service.ServiceName));
            await Task.Delay(1000);
            await RefreshServices();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to stop '{item.DisplayName}'", ex.Message, ex.ToString());
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    private async void StartTaskAction(TaskItemViewModel item)
    {
        try
        {
            item.IsBusy = true;
            await Task.Run(() => _windowsTaskManager.Run(item.Task.TaskPath));
            await Task.Delay(1000);
            await RefreshTasks();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to start '{item.DisplayName}'", ex.Message, ex.ToString());
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    private async void StopTaskAction(TaskItemViewModel item)
    {
        try
        {
            item.IsBusy = true;
            await Task.Run(() => _windowsTaskManager.Stop(item.Task.TaskPath));
            await Task.Delay(1000);
            await RefreshTasks();
        }
        catch (Exception ex)
        {
            await _errorDialog.ShowAsync($"Failed to stop '{item.DisplayName}'", ex.Message, ex.ToString());
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    [ReactiveCommand]
    public void OpenSettings()
    {
        var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
        settingsViewModel.OnSaved = () => SlidePanel.Close();
        SlidePanel.Open("Settings", settingsViewModel);
    }

    private async Task RefreshServices()
    {
        var services = await Task.Run(() => _windowsServiceManager.GetServices().ToList());
        _allServices = services;
        
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedServices = settings.PinnedServices ?? [];
        
        _allServiceViewModels = services.Select(s => new ServiceItemViewModel(
            s,
            pinnedServices.Contains(s.ServiceName),
            TogglePinAction,
            OpenServiceAction,
            StartServiceAction,
            StopServiceAction)).ToList();
            
        FilterServices();
    }

    private async Task RefreshTasks()
    {
        var tasks = await Task.Run(() => _windowsTaskManager.GetTasks().ToList());
        _allTasks = tasks;
        
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        var pinnedTasks = settings.PinnedTasks ?? [];
        
        _allTaskViewModels = tasks.Select(t => new TaskItemViewModel(
            t,
            pinnedTasks.Contains(t.TaskPath),
            TogglePinTaskAction,
            OpenTaskAction,
            StartTaskAction,
            StopTaskAction)).ToList();
            
        FilterTasks();
    }
}