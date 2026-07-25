using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace SystemCtrl.Desktop.Views;

public partial class ErrorDialogWindow : Window
{
    private string _detail = string.Empty;
    private bool _copied;

    public ErrorDialogWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the dialog content. Call before showing.
    /// </summary>
    public void SetContent(string title, string message, string detail)
    {
        _detail = detail;
        TitleText.Text = title;
        MessageText.Text = message;
        DetailText.Text = detail;

        // Hide message label if empty
        MessageText.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;

        await clipboard.SetTextAsync(_detail);

        CopyButtonText.Text = "Copied!";
        CopyIcon.IsVisible = false;
        _copied = true;

        await System.Threading.Tasks.Task.Delay(2000);

        if (_copied)
        {
            CopyButtonText.Text = "Copy to Clipboard";
            CopyIcon.IsVisible = true;
            _copied = false;
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
