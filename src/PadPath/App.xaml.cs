using System.Windows;

namespace PadPath;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "PadPath", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        var config = Services.ConfigService.Load(e.Args);
        Services.ThemeCatalog.Apply(config.Theme, config.Appearance);
        var setupRequested = e.Args.Any(a => a.Equals("--setup", StringComparison.OrdinalIgnoreCase));
        if (Services.ConfigService.NeedsSetup || setupRequested)
        {
            var setup = new SetupWindow(config, firstRun: Services.ConfigService.NeedsSetup);
            if (setup.ShowDialog() != true) { Shutdown(); return; }
            config = setup.Config;
        }
        var window = new MainWindow(config);
        MainWindow = window;
        window.Show();
    }
}
