using System.Collections.ObjectModel;
using System.Windows;
using HandheldLauncher.Models;
using HandheldLauncher.Services;
using Microsoft.Win32;

namespace HandheldLauncher;

public partial class SetupWindow : Window
{
    private readonly ObservableCollection<RootConfig> roots;
    private readonly bool firstRun;
    public LauncherConfig Config { get; }

    public SetupWindow(LauncherConfig config, bool firstRun)
    {
        InitializeComponent();
        this.firstRun = firstRun;
        Config = config;
        roots = new ObservableCollection<RootConfig>(config.Roots);
        FolderList.ItemsSource = roots;
        ConfirmCheck.IsChecked = config.ConfirmBeforeLaunch;
        ExitCheck.IsChecked = config.ExitAfterLaunch;
        HiddenCheck.IsChecked = config.ShowHidden;
        CancelButtonState();
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose a folder containing games", Multiselect = false };
        if (dialog.ShowDialog(this) == true && !roots.Any(r => string.Equals(r.Path, dialog.FolderName, StringComparison.OrdinalIgnoreCase)))
            roots.Add(new RootConfig { Name = new DirectoryInfo(dialog.FolderName).Name, Path = dialog.FolderName });
    }
    private void RemoveFolder_Click(object sender, RoutedEventArgs e) { if (FolderList.SelectedItem is RootConfig root) roots.Remove(root); }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (roots.Count == 0) { MessageBox.Show("Add at least one game folder.", "Folders required", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Config.Roots = roots.ToList(); Config.ConfirmBeforeLaunch = ConfirmCheck.IsChecked == true; Config.ExitAfterLaunch = ExitCheck.IsChecked == true; Config.ShowHidden = HiddenCheck.IsChecked == true;
        ConfigService.Save(Config); DialogResult = true;
    }
    private void SteamButton_Click(object sender, RoutedEventArgs e)
    {
        try { MessageBox.Show(SteamShortcutService.AddLauncher(Environment.ProcessPath!), "Steam", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not add to Steam", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
    private void CancelButtonState() { if (firstRun) Closing += (_, e) => { if (DialogResult != true) e.Cancel = false; }; }
}
