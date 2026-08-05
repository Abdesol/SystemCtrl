using Avalonia;
using Avalonia.Controls;
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

    private void CopyMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var textBox = this.FindControl<TextBox>("LogsTextBox");
        if (textBox != null)
        {
            textBox.Copy();
        }
    }
}
