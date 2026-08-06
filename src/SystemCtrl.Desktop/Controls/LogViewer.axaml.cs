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
                            textBlock.Inlines.Insert(0, CreateInline(line));
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
                            textBlock.Inlines.Add(CreateInline(line));
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
            newInlines.Add(CreateInline(line));
        }

        textBlock.Inlines.AddRange(newInlines);
    }

    private Inline CreateInline(string line)
    {
        var lowerLine = line.ToLowerInvariant();
        string? logClass = null;
        
        if (lowerLine.Contains("error") || lowerLine.Contains("fail") || lowerLine.Contains("exception"))
        {
            logClass = "error";
        }
        else if (lowerLine.Contains("warn"))
        {
            logClass = "warning";
        }
        else if (lowerLine.Contains("info"))
        {
            logClass = "info";
        }
        else if (lowerLine.Contains("success"))
        {
            logClass = "success";
        }

        var match = System.Text.RegularExpressions.Regex.Match(line, @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]");
        if (match.Success)
        {
            var span = new Span();
            var timestampRun = new Run(match.Value + " ");
            timestampRun.Classes.Add("timestamp");
            
            var messageRun = new Run(line.Substring(match.Length).TrimStart() + "\n");
            if (logClass != null) messageRun.Classes.Add(logClass);
            
            span.Inlines.Add(timestampRun);
            span.Inlines.Add(messageRun);
            return span;
        }

        var run = new Run(line + "\n");
        if (logClass != null) run.Classes.Add(logClass);
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
