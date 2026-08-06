using System;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using SystemCtrl.Desktop.Services;

namespace SystemCtrl.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly ISettingsService? _settingsService;
    private readonly Timer _debounceTimer;

    public MainWindow()
    {
        InitializeComponent();
        
        _debounceTimer = new Timer(500);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += DebounceTimer_Elapsed;

        PositionChanged += MainWindow_PositionChanged;
        SizeChanged += MainWindow_SizeChanged;
        PropertyChanged += MainWindow_PropertyChanged;
    }

    public MainWindow(ISettingsService settingsService) : this()
    {
        _settingsService = settingsService;
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (_settingsService == null) return;
        
        var settings = _settingsService.LoadSettings();
        
        if (settings.WindowWidth.HasValue && settings.WindowHeight.HasValue)
        {
            Width = settings.WindowWidth.Value;
            Height = settings.WindowHeight.Value;
        }
        
        if (settings.WindowPositionX.HasValue && settings.WindowPositionY.HasValue)
        {
            Position = new PixelPoint(settings.WindowPositionX.Value, settings.WindowPositionY.Value);
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        if (Enum.TryParse<WindowState>(settings.WindowState, out var state))
        {
            WindowState = state;
        }
    }

    private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            TriggerSave();
        }
    }

    private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        TriggerSave();
    }

    private void MainWindow_PositionChanged(object? sender, PixelPointEventArgs e)
    {
        TriggerSave();
    }

    private void TriggerSave()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void DebounceTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.UIThread.Post(SaveWindowSettings);
    }

    private void SaveWindowSettings()
    {
        if (_settingsService == null) return;

        var settings = _settingsService.LoadSettings();
        
        if (WindowState == WindowState.Normal)
        {
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.WindowPositionX = Position.X;
            settings.WindowPositionY = Position.Y;
        }
        
        settings.WindowState = WindowState.ToString();
        
        _settingsService.SaveSettings(settings);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        SaveWindowSettings();
        base.OnClosing(e);
    }

    private void ChangeWindowPosition(object sender, PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        BeginMoveDrag(e);
    }
}