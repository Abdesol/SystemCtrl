# SystemCtrl

These days, it's rarely malware or viruses that slow down our PCs and eat up our resources. It's the legitimate apps we install. Apps we might only use once a week often register background services and scheduled tasks that quietly run on startup, keeping our devices busy for no reason.

SystemCtrl lets you take back control by showing you exactly which services and tasks are running on your machine. It helps you see what they are doing and gives you a simple interface to stop or disable the ones you don't need.

SystemCtrl is a fast Windows Desktop application built with C# and Avalonia UI. It provides a clean interface for managing Windows Services and background Tasks with high performance and minimal overhead.

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
