using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using PadPath.Models;
using PadPath.Services;

namespace PadPath;

public partial class SetupWindow : Window
{
    private ListBox FolderList => this.FindControl<ListBox>(nameof(FolderList))!;
    private CheckBox ConfirmCheck => this.FindControl<CheckBox>(nameof(ConfirmCheck))!;
    private CheckBox HiddenCheck => this.FindControl<CheckBox>(nameof(HiddenCheck))!;
    private ComboBox ThemeCombo => this.FindControl<ComboBox>(nameof(ThemeCombo))!;
    private ComboBox AppearanceCombo => this.FindControl<ComboBox>(nameof(AppearanceCombo))!;
    private readonly ObservableCollection<RootConfig> roots;
    private readonly string originalTheme;
    private readonly string originalAppearance;
    public LauncherConfig Config { get; }

    public SetupWindow() : this(ConfigService.Load([]), false) { }

    public SetupWindow(LauncherConfig config, bool firstRun)
    {
        InitializeComponent();
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
        Closed += (_, _) => { if (!saved) ThemeCatalog.Apply(originalTheme, originalAppearance); };
    }

    private bool saved;

    private async void AddFolder_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose a folder containing games", AllowMultiple = false });
        var path = folders.FirstOrDefault()?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path) && !roots.Any(r => string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase)))
            roots.Add(new RootConfig { Name = new DirectoryInfo(path).Name, Path = path });
    }
    private void RemoveFolder_Click(object sender, RoutedEventArgs e) { if (FolderList.SelectedItem is RootConfig root) roots.Remove(root); }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (roots.Count == 0) { _ = DialogService.ShowAsync(this, "Folders required", "Add at least one game folder."); return; }
        Config.Roots = roots.ToList(); Config.ConfirmBeforeLaunch = ConfirmCheck.IsChecked == true; Config.ShowHidden = HiddenCheck.IsChecked == true;
        if (ThemeCombo.SelectedItem is ThemeDefinition theme) Config.Theme = theme.Name;
        Config.Appearance = AppearanceCombo.SelectedItem as string ?? "System";
        ConfigService.Save(Config); saved = true; Close(true);
    }
    private async void SteamButton_Click(object? sender, RoutedEventArgs e)
    {
        try { await DialogService.ShowAsync(this, "Steam", SteamShortcutService.AddLauncher(Environment.ProcessPath!)); }
        catch (Exception ex) { await DialogService.ShowAsync(this, "Could not add to Steam", ex.Message); }
    }
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
    private void ThemeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateThemePreview();
    }
    private void AppearanceCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateThemePreview();
    private void UpdateThemePreview()
    {
        if (ThemeCombo?.SelectedItem is not ThemeDefinition theme || AppearanceCombo?.SelectedItem is not string appearance) return;
        ThemeCatalog.Apply(theme.Name, appearance);
    }
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
