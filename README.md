# SystemCtrl

These days, it is rarely malware or viruses that slow down our devices. It is the legitimate apps we install. Apps we barely use often register background services and scheduled tasks. Disabling them from the Task Manager's Startup Apps tab is not enough. The Services and Task Scheduler apps on Windows are also hard to navigate.

SystemCtrl is a Windows desktop app built with C# and Avalonia for managing Windows Services and background tasks. It gives you a unified interface to see what's running and understand what it does. No unnecessary info, just what you need to stop or disable the things you don't want, with minimal overhead.

## Features
- **Manage Windows Services**: View, start, stop, and filter system services.
- **Manage Scheduled Tasks**: Interactively view and manage background tasks.
- **Pin Favorites**: Keep your most-used services or tasks pinned to the top. It is perfect for when you frequently want to quickly enable or disable them.
- **AI Summary**: Uses AI to explain what a service or task does in plain English, helping you decide if it is safe to disable. *(Note: This feature uses Google Gemini and requires you to provide your own Gemini API key in the application settings).*

## Installation

> **Note**: SystemCtrl is currently only available for Windows.

You can download the latest version of SystemCtrl from the [Releases](https://github.com/Abdesol/SystemCtrl/releases) page.

1. **Installer**: Download `SystemCtrl-Setup.exe` to install it normally (adds a Start Menu shortcut and uninstaller).
2. **Portable**: Download `SystemCtrl.exe` and run it from anywhere without installation.

## Building from Source

To build SystemCtrl yourself, you need the [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.

```bash
# Clone the repository
git clone https://github.com/abdesol/SystemCtrl.git

# Navigate to the source folder
cd SystemCtrl

# Build and Publish for Windows x64
dotnet publish "src\SystemCtrl.Desktop\SystemCtrl.Desktop.csproj" -c Release -r win-x64
```
The compiled executable will be located in `src\SystemCtrl.Desktop\bin\Release\net10.0-windows\win-x64\publish\`.

## License
This project is licensed under the [MIT License](LICENSE).
