using System.Threading.Tasks;

namespace SystemCtrl.Desktop.Services;

public interface IErrorDialogService
{
    Task ShowAsync(string title, string message, string detail);
}
