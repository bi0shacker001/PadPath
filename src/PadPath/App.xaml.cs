using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PadPath.Services;

namespace PadPath;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            var config = ConfigService.Load(args);
            ThemeCatalog.Apply(config.Theme, config.Appearance);
            var setupRequested = args.Any(a => a.Equals("--setup", StringComparison.OrdinalIgnoreCase));
            var selectorMode = args.Any(a => a.Equals("--selector", StringComparison.OrdinalIgnoreCase));
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = new MainWindow(config, selectorMode, setupRequested || ConfigService.NeedsSetup);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
