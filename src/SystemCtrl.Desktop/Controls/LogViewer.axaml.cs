using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SystemCtrl.Desktop.Controls;

public partial class LogViewer : UserControl
{
    public static readonly StyledProperty<string> LogsTextProperty =
        AvaloniaProperty.Register<LogViewer, string>(nameof(LogsText));

    public string LogsText
    {
        get => GetValue(LogsTextProperty);
        set => SetValue(LogsTextProperty, value);
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
        
        if (change.Property == LogsTextProperty)
        {
            ParseAndRenderLogs(change.NewValue as string);
        }
    }

    private void ParseAndRenderLogs(string? text)
    {
        var textBlock = this.FindControl<SelectableTextBlock>("LogsTextBlock");
        if (textBlock == null) return;
        
        var inlines = textBlock.Inlines;
        if (inlines == null) return;
        
        inlines.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        var newInlines = new List<Inline>();
        using var reader = new StringReader(text);
        
        string? line;
        while ((line = reader.ReadLine()) != null)
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
            
            newInlines.Add(run);
        }

        inlines.AddRange(newInlines);
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
