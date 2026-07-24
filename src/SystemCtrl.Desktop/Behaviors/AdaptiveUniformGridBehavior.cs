using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace SystemCtrl.Desktop.Behaviors;

public class AdaptiveUniformGridBehavior : Behavior<UniformGrid>
{
    public static readonly StyledProperty<double> MinItemWidthProperty =
        AvaloniaProperty.Register<AdaptiveUniformGridBehavior, double>(
            nameof(MinItemWidth), 300);

    public static readonly StyledProperty<int> MaxColumnsProperty =
        AvaloniaProperty.Register<AdaptiveUniformGridBehavior, int>(
            nameof(MaxColumns), 5);

    public double MinItemWidth
    {
        get => GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public int MaxColumns
    {
        get => GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is null)
            return;

        AssociatedObject.SizeChanged += OnSizeChanged;
        UpdateColumns();
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
            AssociatedObject.SizeChanged -= OnSizeChanged;

        base.OnDetaching();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateColumns();
    }

    private void UpdateColumns()
    {
        if (AssociatedObject is null)
            return;

        AssociatedObject.Columns = Math.Clamp(
            (int)(AssociatedObject.Bounds.Width / MinItemWidth),
            1,
            MaxColumns);
    }
}