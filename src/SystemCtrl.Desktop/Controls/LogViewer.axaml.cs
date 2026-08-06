using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SystemCtrl.Desktop.Controls;

public partial class LogViewer : UserControl
{
    public static readonly StyledProperty<ObservableCollection<string>> LogsListProperty =
        AvaloniaProperty.Register<LogViewer, ObservableCollection<string>>(nameof(LogsList));

    public ObservableCollection<string> LogsList
    {
        get => GetValue(LogsListProperty);
        set => SetValue(LogsListProperty, value);
    }

    public LogViewer()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == LogsListProperty)
        {
            if (change.OldValue is ObservableCollection<string> oldList)
            {
                oldList.CollectionChanged -= LogsList_CollectionChanged;
            }
            if (change.NewValue is ObservableCollection<string> newList)
            {
                newList.CollectionChanged += LogsList_CollectionChanged;
                ParseAndRenderAll(newList);
            }
        }
    }

    private void LogsList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var textBlock = this.FindControl<SelectableTextBlock>("LogsTextBlock");
            if (textBlock == null || textBlock.Inlines == null) return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                textBlock.Inlines.Clear();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                if (e.NewStartingIndex == 0)
                {
                    // Items added to the top
                    for (int i = e.NewItems.Count - 1; i >= 0; i--)
                    {
                        if (e.NewItems[i] is string line)
                        {
                            textBlock.Inlines.Insert(0, CreateRun(line));
                        }
                    }
                }
                else
                {
                    // Fallback for appending
                    foreach (var item in e.NewItems)
                    {
                        if (item is string line)
                        {
                            textBlock.Inlines.Add(CreateRun(line));
                        }
                    }
                }
            }
        });
    }

    private void ParseAndRenderAll(IEnumerable<string> lines)
    {
        var textBlock = this.FindControl<SelectableTextBlock>("LogsTextBlock");
        if (textBlock == null || textBlock.Inlines == null) return;
        
        textBlock.Inlines.Clear();
        
        if (lines == null) return;

        var newInlines = new List<Inline>();
        foreach (var line in lines)
        {
            newInlines.Add(CreateRun(line));
        }

        textBlock.Inlines.AddRange(newInlines);
    }

    private Run CreateRun(string line)
    {
        var run = new Run(line + "\n");
        var lowerLine = line.ToLowerInvariant();
            
            if (lowerLine.Contains("error") || lowerLine.Contains("fail") || lowerLine.Contains("exception"))
            {
                run.Classes.Add("error");
            }
            else if (lowerLine.Contains("warn"))
            {
                run.Classes.Add("warning");
            }
            else if (lowerLine.Contains("info"))
            {
                run.Classes.Add("info");
            }
            else if (lowerLine.Contains("success"))
            {
                run.Classes.Add("success");
            }
            
        return run;
    }

    private void CopyMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var textBlock = this.FindControl<SelectableTextBlock>("LogsTextBlock");
        if (textBlock != null)
        {
            textBlock.Copy();
        }
    }
}
