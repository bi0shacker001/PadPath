using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using PadPath.Models;
using PadPath.Services;

namespace PadPath;

public partial class SetupWindow : Window
{
    private readonly ObservableCollection<RootConfig> roots;
    private readonly bool firstRun;
    private readonly string originalTheme;
    private readonly string originalAppearance;
    public LauncherConfig Config { get; }

    public SetupWindow(LauncherConfig config, bool firstRun)
    {
        InitializeComponent();
        this.firstRun = firstRun;
        originalTheme = config.Theme;
        originalAppearance = config.Appearance;
        Config = config;
        roots = new ObservableCollection<RootConfig>(config.Roots);
        FolderList.ItemsSource = roots;
        ConfirmCheck.IsChecked = config.ConfirmBeforeLaunch;
        HiddenCheck.IsChecked = config.ShowHidden;
        ThemeCombo.ItemsSource = ThemeCatalog.All;
        ThemeCombo.SelectedItem = ThemeCatalog.All.FirstOrDefault(t => t.Name.Equals(config.Theme, StringComparison.OrdinalIgnoreCase)) ?? ThemeCatalog.All[0];
        AppearanceCombo.ItemsSource = ThemeCatalog.Appearances;
        AppearanceCombo.SelectedItem = ThemeCatalog.Appearances.FirstOrDefault(a => a.Equals(config.Appearance, StringComparison.OrdinalIgnoreCase)) ?? "System";
        UpdateThemePreview();
        CancelButtonState();
        Closed += (_, _) => { if (DialogResult != true) ThemeCatalog.Apply(originalTheme, originalAppearance); };
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
        Config.Roots = roots.ToList(); Config.ConfirmBeforeLaunch = ConfirmCheck.IsChecked == true; Config.ShowHidden = HiddenCheck.IsChecked == true;
        if (ThemeCombo.SelectedItem is ThemeDefinition theme) Config.Theme = theme.Name;
        Config.Appearance = AppearanceCombo.SelectedItem as string ?? "System";
        ConfigService.Save(Config); DialogResult = true;
    }
    private void SteamButton_Click(object sender, RoutedEventArgs e)
    {
        try { MessageBox.Show(SteamShortcutService.AddLauncher(Environment.ProcessPath!), "Steam", MessageBoxButton.OK, MessageBoxImage.Information); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not add to Steam", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
    private void ThemeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateThemePreview();
    }
    private void AppearanceCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateThemePreview();
    private void UpdateThemePreview()
    {
        if (ThemeCombo?.SelectedItem is not ThemeDefinition theme || AppearanceCombo?.SelectedItem is not string appearance) return;
        ThemeCatalog.Apply(theme.Name, appearance);
    }
    private void CancelButtonState() { if (firstRun) Closing += (_, e) => { if (DialogResult != true) e.Cancel = false; }; }
}
