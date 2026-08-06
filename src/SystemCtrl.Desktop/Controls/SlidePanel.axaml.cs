using System;
using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.VisualTree;
using ReactiveUI;
using SystemCtrl.Desktop.ViewModels;

namespace SystemCtrl.Desktop.Controls;

public partial class SlidePanel : UserControl
{
    private readonly Transitions _transitions =
    [
        new TransformOperationsTransition
        {
            Property = RenderTransformProperty,
            Duration = TimeSpan.FromMilliseconds(380),
            Easing = new CubicEaseOut()
        }
    ];

    private Border? _panelRoot;
    private bool? _previousIsOpen;
    private IDisposable? _subscription;

    public SlidePanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _subscription?.Dispose();
        if (DataContext is SlidePanelViewModel vm)
            _subscription = vm.WhenAnyValue(x => x.IsOpen)
                              .Subscribe(_ => AnimateSlide());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _panelRoot = this.FindControl<Border>("PanelRoot");
        if (_panelRoot is not null)
            _panelRoot.LayoutUpdated += OnPanelLayoutUpdated;

        var splitter = this.FindControl<GridSplitter>("PanelSplitter");
        if (splitter != null)
            splitter.AddHandler(PointerReleasedEvent, OnSplitterPointerReleased, Avalonia.Interactivity.RoutingStrategies.Bubble, true);
    }

    private void OnSplitterPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is SlidePanelViewModel vm)
        {
            var rootGrid = this.FindControl<Grid>("RootGrid");
            var panelColumn = rootGrid?.ColumnDefinitions[2];
            if (panelColumn is { Width.IsAbsolute: true })
            {
                vm.PanelWidth = panelColumn.Width.Value;
            }
            vm.SaveWidth();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _subscription?.Dispose();
        if (_panelRoot is not null)
            _panelRoot.LayoutUpdated -= OnPanelLayoutUpdated;

        var splitter = this.FindControl<GridSplitter>("PanelSplitter");
        if (splitter != null)
            splitter.RemoveHandler(PointerReleasedEvent, OnSplitterPointerReleased);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty)
        {
            var bounds = change.GetNewValue<Rect>();
            if (bounds.Width > 0)
            {
                var rootGrid = this.FindControl<Grid>("RootGrid");
                var panelColumn = rootGrid?.ColumnDefinitions[2];
                if (panelColumn is { Width.IsAbsolute: true })
                {
                    panelColumn.MaxWidth = Math.Max(300, bounds.Width - 56);
                }
            }
        }
    }

    private double _lastTx = -1;

    private void OnPanelLayoutUpdated(object? sender, EventArgs e)
    {
        if (_panelRoot is null) return;

        var width = _panelRoot.Bounds.Width;
        if (width <= 0) return;

        var vm = DataContext as SlidePanelViewModel;

        // When Splitter is dragged, actual width changes. Sync back if it's smaller than the actual requested VM width
        // Wait, if we use MaxWidth on the column, the actual width might be clamped by MaxWidth.
        // We only want to sync back to ViewModel if the user actively dragged the splitter to this width.
        // But since GridSplitter sets the Column Width directly, the TwoWay binding will push it.
        // We don't need to manually set vm.PanelWidth = width here anymore because 
        // the TwoWay binding handles GridSplitter dragging, and we removed the layout loop risk!
        
        var isOpen = vm != null && vm.IsOpen;
        var tx = isOpen ? 0d : width;

        if (Math.Abs(_lastTx - tx) < 0.1) return;
        _lastTx = tx;

        _panelRoot.RenderTransform = TransformOperations.Parse(
            $"translate({tx.ToString(CultureInfo.InvariantCulture)}px, 0px)");
    }

    private void AnimateSlide()
    {
        if (_panelRoot is null) return;
        if (DataContext is not SlidePanelViewModel vm) return;
        if (_previousIsOpen == vm.IsOpen) return;

        _previousIsOpen = vm.IsOpen;

        // Prefer measured bounds. fall back to the styled Width property
        var width = _panelRoot.Bounds.Width > 0
            ? _panelRoot.Bounds.Width
            : _panelRoot.Width;

        if (double.IsNaN(width) || width <= 0) return;

        var toX = vm.IsOpen ? 0d : width;

        _panelRoot.Transitions ??= _transitions;
        _panelRoot.RenderTransform = TransformOperations.Parse(
            $"translate({toX.ToString(CultureInfo.InvariantCulture)}px, 0px)");
    }

    private void DimOverlay_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is SlidePanelViewModel vm)
            vm.Close();
    }
}
