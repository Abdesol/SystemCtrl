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
using Avalonia.Media;

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

    private class MatchInfo
    {
        public int LineIndex { get; set; }
        public int CharOffset { get; set; }
        public Run HighlightRun { get; set; }
        public MatchInfo(int lineIndex, int charOffset, Run highlightRun)
        {
            LineIndex = lineIndex;
            CharOffset = charOffset;
            HighlightRun = highlightRun;
        }
    }

    private double _charWidth = -1;
    
    private double GetCharWidth()
    {
        if (_charWidth > 0) return _charWidth;
        
        var textBlock = this.FindControl<SelectableTextBlock>("LogsTextBlock");
        if (textBlock == null) return 7.2;

        var typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight);
        
        var formattedText = new FormattedText(
            "A", 
            System.Globalization.CultureInfo.CurrentCulture, 
            FlowDirection.LeftToRight, 
            typeface, 
            textBlock.FontSize, 
            null);
            
        _charWidth = formattedText.Width;
        return _charWidth;
    }

    private string _searchQuery = string.Empty;
    private List<MatchInfo> _matches = new();
    private int _currentMatchIndex = -1;
    private Run? _lastActiveRun;
    
    private readonly IBrush _normalMatchBrush = new SolidColorBrush(Color.Parse("#40FFFF00"));
    private readonly IBrush _activeMatchBrush = new SolidColorBrush(Color.Parse("#90FFA500"));
    
    private TextBox? _searchTextBox;
    private TextBlock? _matchCountTextBlock;
    private ScrollViewer? _logScrollViewer;
    private Button? _clearSearchButton;

    public LogViewer()
    {
        InitializeComponent();
        
        _searchTextBox = this.FindControl<TextBox>("SearchTextBox");
        _matchCountTextBlock = this.FindControl<TextBlock>("MatchCountTextBlock");
        _logScrollViewer = this.FindControl<ScrollViewer>("LogScrollViewer");
        _clearSearchButton = this.FindControl<Button>("ClearSearchButton");

        if (_clearSearchButton != null)
        {
            _clearSearchButton.IsVisible = false;
        }

        UpdateMatchCountText();

        if (_searchTextBox != null)
        {
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;
        }
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

    private void SearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_searchTextBox == null) return;
        _searchQuery = _searchTextBox.Text ?? string.Empty;
        
        if (_clearSearchButton != null)
        {
            _clearSearchButton.IsVisible = !string.IsNullOrEmpty(_searchQuery);
        }

        ParseAndRenderAll(LogsList);
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
                _matches.Clear();
                _currentMatchIndex = -1;
                _lastActiveRun = null;
                UpdateMatchCountText();
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
                            var inline = CreateInline(line, out var lineMatches);
                            textBlock.Inlines.Insert(0, inline);
                            
                            foreach (var m in _matches)
                            {
                                m.LineIndex++;
                            }
                            
                            if (lineMatches.Count > 0)
                            {
                                var newMatchInfos = new List<MatchInfo>();
                                foreach(var matchData in lineMatches)
                                {
                                    newMatchInfos.Add(new MatchInfo(0, matchData.CharOffset, matchData.Run));
                                }
                                _matches.InsertRange(0, newMatchInfos);
                                
                                if (_currentMatchIndex >= 0)
                                {
                                    _currentMatchIndex += lineMatches.Count;
                                }
                            }
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
                            int newLineIndex = textBlock.Inlines.Count;
                            var inline = CreateInline(line, out var lineMatches);
                            
                            foreach(var matchData in lineMatches)
                            {
                                _matches.Add(new MatchInfo(newLineIndex, matchData.CharOffset, matchData.Run));
                            }
                            textBlock.Inlines.Add(inline);
                        }
                    }
                }
                UpdateMatchCountText();
            }
        });
    }

    private void ParseAndRenderAll(IEnumerable<string>? lines)
    {
        var textBlock = this.FindControl<SelectableTextBlock>("LogsTextBlock");
        if (textBlock == null || textBlock.Inlines == null) return;
        
        textBlock.Inlines.Clear();
        _matches.Clear();
        _currentMatchIndex = -1;
        _lastActiveRun = null;
        
        if (lines == null) return;

        var newInlines = new List<Inline>();
        int lineIndex = 0;
        foreach (var line in lines)
        {
            var inline = CreateInline(line, out var lineMatches);
            foreach(var matchData in lineMatches)
            {
                _matches.Add(new MatchInfo(lineIndex, matchData.CharOffset, matchData.Run));
            }
            newInlines.Add(inline);
            lineIndex++;
        }

        textBlock.Inlines.AddRange(newInlines);
        
        if (_matches.Count > 0 && !string.IsNullOrEmpty(_searchQuery))
        {
            _currentMatchIndex = 0;
            UpdateActiveMatchHighlight();
            ScrollToCurrentMatch();
        }
        UpdateMatchCountText();
    }

    private Inline CreateInline(string line, out List<(Run Run, int CharOffset)> lineMatches)
    {
        string? logClass = null;
        lineMatches = new List<(Run, int)>();
        
        if (line.Contains("[Critical]") || line.Contains("[Error]"))
        {
            logClass = "error";
        }
        else if (line.Contains("[Warn]"))
        {
            logClass = "warning";
        }
        else if (line.Contains("[Info]") || line.Contains("[Verbose]"))
        {
            logClass = "info";
        }
        else
        {
            var lowerLine = line.ToLowerInvariant();
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
        }

        var match = System.Text.RegularExpressions.Regex.Match(line, @"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\]");
        
        var span = new Span();

        if (match.Success)
        {
            var timestampText = match.Value + " ";
            var messageText = line.Substring(match.Length) + "\n";
            
            span.Inlines.AddRange(CreateSearchHighlightedInlines(timestampText, "timestamp", lineMatches, 0));
            span.Inlines.AddRange(CreateSearchHighlightedInlines(messageText, logClass, lineMatches, timestampText.Length));
        }
        else
        {
            span.Inlines.AddRange(CreateSearchHighlightedInlines(line + "\n", logClass, lineMatches, 0));
        }

        return span;
    }

    private IEnumerable<Inline> CreateSearchHighlightedInlines(string text, string? cssClass, List<(Run Run, int CharOffset)> lineMatches, int startOffset)
    {
        if (string.IsNullOrEmpty(_searchQuery))
        {
            var run = new Run(text);
            if (cssClass != null) run.Classes.Add(cssClass);
            return new[] { run };
        }

        var inlines = new List<Inline>();
        int currentIndex = 0;
        
        while (currentIndex < text.Length)
        {
            int matchIndex = text.IndexOf(_searchQuery, currentIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex == -1)
            {
                var run = new Run(text.Substring(currentIndex));
                if (cssClass != null) run.Classes.Add(cssClass);
                inlines.Add(run);
                break;
            }

            if (matchIndex > currentIndex)
            {
                var run = new Run(text.Substring(currentIndex, matchIndex - currentIndex));
                if (cssClass != null) run.Classes.Add(cssClass);
                inlines.Add(run);
            }

            var highlightRun = new Run(text.Substring(matchIndex, _searchQuery.Length));
            if (cssClass != null) highlightRun.Classes.Add(cssClass);
            
            highlightRun.Background = _normalMatchBrush;
            
            inlines.Add(highlightRun);
            lineMatches.Add((highlightRun, startOffset + matchIndex));
            
            currentIndex = matchIndex + _searchQuery.Length;
        }

        return inlines;
    }

    private void UpdateActiveMatchHighlight()
    {
        if (_lastActiveRun != null)
        {
            _lastActiveRun.Background = _normalMatchBrush;
        }
        
        if (_currentMatchIndex >= 0 && _currentMatchIndex < _matches.Count)
        {
            var activeRun = _matches[_currentMatchIndex].HighlightRun;
            activeRun.Background = _activeMatchBrush;
            _lastActiveRun = activeRun;
        }
        else
        {
            _lastActiveRun = null;
        }
    }

    private void PreviousMatch_Click(object? sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0) return;
        _currentMatchIndex--;
        if (_currentMatchIndex < 0) _currentMatchIndex = _matches.Count - 1;
        UpdateActiveMatchHighlight();
        UpdateMatchCountText();
        ScrollToCurrentMatch();
    }

    private void NextMatch_Click(object? sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0) return;
        _currentMatchIndex++;
        if (_currentMatchIndex >= _matches.Count) _currentMatchIndex = 0;
        UpdateActiveMatchHighlight();
        UpdateMatchCountText();
        ScrollToCurrentMatch();
    }

    private void UpdateMatchCountText()
    {
        if (_matchCountTextBlock == null) return;
        if (string.IsNullOrEmpty(_searchQuery))
        {
            _matchCountTextBlock.Text = "";
            _matchCountTextBlock.IsVisible = false;
        }
        else if (_matches.Count == 0)
        {
            _matchCountTextBlock.Text = "0/0";
            _matchCountTextBlock.IsVisible = false;
        }
        else
        {
            _matchCountTextBlock.Text = $"{_currentMatchIndex + 1}/{_matches.Count}";
            _matchCountTextBlock.IsVisible = true;
        }
    }

    private void ScrollToCurrentMatch()
    {
        if (_logScrollViewer == null || _currentMatchIndex < 0 || _currentMatchIndex >= _matches.Count) return;
        
        var match = _matches[_currentMatchIndex];
        
        double targetY = match.LineIndex * 18.0;
        targetY = Math.Max(0, targetY - 36.0); 
        
        double targetX = match.CharOffset * GetCharWidth();
        double simpleNewX = Math.Max(0, targetX - 50.0);
        
        _logScrollViewer.Offset = new Vector(simpleNewX, targetY);
    }

    private void CopyMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var textBlock = this.FindControl<SelectableTextBlock>("LogsTextBlock");
        if (textBlock != null)
        {
            textBlock.Copy();
        }
    }

    private void ClearSearch_Click(object? sender, RoutedEventArgs e)
    {
        if (_searchTextBox != null)
        {
            _searchTextBox.Text = string.Empty;
        }
    }
}
