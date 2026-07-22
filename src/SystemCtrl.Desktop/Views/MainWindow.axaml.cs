using Avalonia.Controls;
using Avalonia.Input;

namespace SystemCtrl.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void ChangeWindowPosition(object sender, PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        BeginMoveDrag(e);
    }
}