using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using SystemCtrl.Desktop.Services;
using SystemCtrl.Desktop.ViewModels;
using SystemCtrl.Desktop.Views;

namespace SystemCtrl.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ConfigureFluentThemePalettes();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddServices();
        
        var services = collection.BuildServiceProvider();

        var settingsService = services.GetRequiredService<ISettingsService>();
        var settings = settingsService.LoadSettings();
        SetTheme(settings.AppTheme);

        var vm = services.GetRequiredService<MainViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(settingsService)
            {
                DataContext = vm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void SetTheme(string theme)
    {
        if (Current is not App app)
            return;

        app.RequestedThemeVariant = theme == "Dark"
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }

    private void ConfigureFluentThemePalettes()
    {
        var fluentTheme = Styles.OfType<FluentTheme>().FirstOrDefault();
        if (fluentTheme == null)
            return;

        fluentTheme.Palettes[ThemeVariant.Light] = BuildPalette(ThemeVariant.Light);
        fluentTheme.Palettes[ThemeVariant.Dark] = BuildPalette(ThemeVariant.Dark);
    }

    private ColorPaletteResources BuildPalette(ThemeVariant theme)
    {
        Color C(string key)
        {
            if (Resources.TryGetResource(key, theme, out var value) && value is Color color)
                return color;

            return default;
        }

        return new ColorPaletteResources
        {
            Accent = C("AccentFillDefaultColor"),

            AltHigh = C("ControlFillDefaultColor"),
            AltLow = C("BackgroundColor"),
            AltMedium = C("NavigationViewBackgroundColor"),
            AltMediumHigh = C("ControlFillDefaultColor"),
            AltMediumLow = C("AltMediumLowColor"),

            BaseHigh = C("TextFillPrimaryColor"),
            BaseLow = C("ControlFillSecondaryColor"),
            BaseMedium = C("TextFillTertiaryColor"),
            BaseMediumHigh = C("BaseMediumHighColor"),
            BaseMediumLow = C("BaseMediumLowColor"),

            ChromeAltLow = C("TextFillPrimaryColor"),
            ChromeBlackHigh = C("ControlStrokeDefaultColor"),
            ChromeBlackLow = C("ChromeBlackLowColor"),
            ChromeBlackMedium = C("ChromeBlackMediumColor"),
            ChromeBlackMediumLow = C("ChromeBlackMediumLowColor"),
            ChromeDisabledHigh = C("ChromeDisabledHighColor"),
            ChromeDisabledLow = C("ChromeDisabledLowColor"),
            ChromeGray = C("ChromeGrayColor"),

            ChromeHigh = C("FocusStrokeColor"),
            ChromeLow = C("BackgroundColor"),
            ChromeMedium = C("ControlFillTertiaryColor"),
            ChromeMediumLow = C("ControlFillSecondaryColor"),
            ChromeWhite = C("ControlFillDefaultColor"),

            ListLow = C("ControlFillSecondaryColor"),
            ListMedium = C("FocusStrokeColor"),

            RegionColor = C("BackgroundColor"),

            ErrorText = C("ErrorColor"),
        };
    }
}