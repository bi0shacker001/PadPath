using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using PadPath.Input;
using PadPath.Models;
using PadPath.Services;

namespace PadPath;

public partial class MainWindow : Window
{
    private TextBlock TitleText => this.FindControl<TextBlock>(nameof(TitleText))!;
    private TextBlock BreadcrumbText => this.FindControl<TextBlock>(nameof(BreadcrumbText))!;
    private TextBlock CountText => this.FindControl<TextBlock>(nameof(CountText))!;
    private TextBlock ControllerText => this.FindControl<TextBlock>(nameof(ControllerText))!;
    private TextBlock ConfirmationName => this.FindControl<TextBlock>(nameof(ConfirmationName))!;
    private Avalonia.Controls.Shapes.Ellipse ControllerDot => this.FindControl<Avalonia.Controls.Shapes.Ellipse>(nameof(ControllerDot))!;
    private Button OpenPrompt => this.FindControl<Button>(nameof(OpenPrompt))!;
    private Button LaunchButton => this.FindControl<Button>(nameof(LaunchButton))!;
    private ListBox BrowserList => this.FindControl<ListBox>(nameof(BrowserList))!;
    private StackPanel RootButtons => this.FindControl<StackPanel>(nameof(RootButtons))!;
    private Border EmptyPanel => this.FindControl<Border>(nameof(EmptyPanel))!;
    private Border ConfirmationPanel => this.FindControl<Border>(nameof(ConfirmationPanel))!;
    private readonly LauncherConfig config;
    private readonly bool selectorMode;
    private readonly FileBrowserService browser;
    private readonly GamepadController? controller;
    private readonly bool showSetupOnOpen;
    private readonly ObservableCollection<BrowserItem> items = [];
    private RootConfig activeRoot = null!;
    private string currentDirectory = "";
    private string? pendingLaunchPath;
    private bool selectionReturned;
    private int rootIndex;

    public MainWindow() : this(ConfigService.Load([])) { }

    public MainWindow(LauncherConfig config, bool selectorMode = false, bool showSetupOnOpen = false)
    {
        this.config = config;
        this.selectorMode = selectorMode;
        this.showSetupOnOpen = showSetupOnOpen;
        browser = new FileBrowserService(config);
        if (!string.Equals(Environment.GetEnvironmentVariable("HANDHELD_LAUNCHER_DISABLE_CONTROLLER"), "1", StringComparison.Ordinal))
            controller = new GamepadController();
        InitializeComponent();
        TitleText.Text = selectorMode ? "Select an executable" : config.Title;
        if (selectorMode) OpenPrompt.Content = "Ⓐ SELECT";
        BrowserList.ItemsSource = items;
        BuildRootButtons();
        if (controller is not null) controller.Pressed += HandleGamepad;
        Closed += (_, _) =>
        {
            controller?.Dispose();
            if (selectorMode && !selectionReturned && Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown(1);
        };
        Opened += async (_, _) =>
        {
            if (showSetupOnOpen)
            {
                var setup = new SetupWindow(config, firstRun: true);
                if (!await setup.ShowDialog<bool>(this)) { Close(); return; }
                BuildRootButtons();
            }
            if (config.Fullscreen) WindowState = WindowState.FullScreen;
            OpenInitialFolder();
            BrowserList.Focus();
        };
        var statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        statusTimer.Tick += UpdateControllerStatus;
        statusTimer.Start();
    }

    private void OpenInitialFolder()
    {
        var last = config.RememberLastFolder ? ConfigService.LoadLastFolder() : null;
        var matchingRoot = last is null ? null : config.Roots.FirstOrDefault(r =>
        {
            var path = Environment.ExpandEnvironmentVariables(r.Path);
            return Directory.Exists(path) && FileBrowserService.IsWithinRoot(last, path);
        });
        if (matchingRoot is not null && Directory.Exists(last)) OpenRoot(config.Roots.IndexOf(matchingRoot), last);
        else OpenRoot(0);
    }

    private void BuildRootButtons()
    {
        RootButtons.Children.Clear();
        for (var i = 0; i < config.Roots.Count; i++)
        {
            var index = i;
            var button = new Button { Content = config.Roots[i].Name, Tag = i };
            button.Classes.Add("rootButton");
            button.Click += (_, _) => OpenRoot(index);
            RootButtons.Children.Add(button);
        }
    }

    private void OpenRoot(int index, string? directory = null)
    {
        var candidateRoot = config.Roots[index];
        var rootPath = Environment.ExpandEnvironmentVariables(candidateRoot.Path);
        if (!Directory.Exists(rootPath))
        {
            _ = DialogService.ShowAsync(this, "Folder unavailable", $"The configured folder does not exist:\n{rootPath}");
            return;
        }
        rootIndex = index;
        activeRoot = candidateRoot;
        Navigate(directory ?? rootPath);
    }

    private void Navigate(string directory)
    {
        var rootPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(activeRoot.Path));
        var destination = Path.GetFullPath(directory);
        if (!FileBrowserService.IsWithinRoot(destination, rootPath)) destination = rootPath;
        currentDirectory = destination;
        items.Clear();
        foreach (var item in browser.ReadDirectory(destination, rootPath)) items.Add(item);
        var relative = Path.GetRelativePath(rootPath, destination);
        BreadcrumbText.Text = relative == "." ? activeRoot.Name : $"{activeRoot.Name}  /  {relative.Replace('\\', '/')}";
        CountText.Text = $"{items.Count(i => !i.IsParent)} ITEMS";
        EmptyPanel.IsVisible = items.Count == 0;
        if (items.Count > 0) BrowserList.SelectedIndex = 0;
        if (config.RememberLastFolder) ConfigService.SaveLastFolder(destination);
        UpdateRootButtonState();
    }

    private void UpdateRootButtonState()
    {
        foreach (Button button in RootButtons.Children)
        {
            var active = button.Tag is int index && index == rootIndex;
            button.BorderBrush = ResourceBrush("BorderBrush");
            button.Background = ResourceBrush(active ? "AccentBrush" : "PanelBrush");
            button.Foreground = ResourceBrush(active ? "AccentTextBrush" : "TextBrush");
        }
    }

    private void ActivateSelection()
    {
        if (BrowserList.SelectedItem is not BrowserItem item) return;
        if (item.IsDirectory) Navigate(item.FullPath);
        else if (selectorMode) ReturnSelection(item.FullPath);
        else Launch(item.FullPath);
    }

    private void ReturnSelection(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var directoryPath = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var result = new SelectorResult(
            directoryPath,
            fullPath,
            Path.GetFileName(fullPath),
            Path.GetFileName(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
        var json = JsonSerializer.Serialize(result);
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        writer.WriteLine(json);
        selectionReturned = true;
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown(0);
    }

    private void Launch(string path)
    {
        if (config.ConfirmBeforeLaunch)
        {
            pendingLaunchPath = path;
            ConfirmationName.Text = Path.GetFileNameWithoutExtension(path);
            ConfirmationPanel.IsVisible = true;
            LaunchButton.Focus();
            return;
        }
        LaunchNow(path);
    }

    private async void LaunchNow(string path)
    {
        try
        {
            var target = TargetLauncher.Launch(path);
            WindowState = WindowState.Minimized;

            var grace = Task.Delay(TimeSpan.FromSeconds(Math.Clamp(config.MinimumHandoffSeconds, 5, 120)));
            try { await Task.WhenAll(target.WaitForExitAsync(), grace); }
            catch (InvalidOperationException) { await grace; }

            WindowState = config.Fullscreen ? WindowState.FullScreen : WindowState.Normal;
            Activate();
            BrowserList.Focus();
        }
        catch (Exception ex) { WindowState = config.Fullscreen ? WindowState.FullScreen : WindowState.Normal; await DialogService.ShowAsync(this, "Launch failed", ex.Message); }
    }

    private void GoBack()
    {
        var parent = Directory.GetParent(currentDirectory)?.FullName;
        var rootPath = Environment.ExpandEnvironmentVariables(activeRoot.Path);
        if (parent is not null && FileBrowserService.IsWithinRoot(parent, rootPath)) Navigate(parent);
    }

    private void HandleGamepad(GamepadAction action) => Dispatcher.UIThread.Post(() =>
    {
        if (action == GamepadAction.Quit) { Close(); return; }
        if (ConfirmationPanel.IsVisible)
        {
            if (action == GamepadAction.Accept && pendingLaunchPath is not null) ConfirmLaunch();
            else if (action is GamepadAction.Back or GamepadAction.Quit) CancelLaunch();
            return;
        }
        switch (action)
        {
            case GamepadAction.Up: MoveSelection(-1); break;
            case GamepadAction.Down: MoveSelection(1); break;
            case GamepadAction.Left: MoveSelection(-5); break;
            case GamepadAction.Right: MoveSelection(5); break;
            case GamepadAction.Accept: ActivateSelection(); break;
            case GamepadAction.Back: GoBack(); break;
            case GamepadAction.Roots: OpenRoot((rootIndex + 1) % config.Roots.Count); break;
            case GamepadAction.Settings: OpenSettings(); break;
        }
    });

    private void MoveSelection(int delta)
    {
        if (items.Count == 0) return;
        BrowserList.SelectedIndex = Math.Clamp(BrowserList.SelectedIndex + delta, 0, items.Count - 1);
        if (BrowserList.SelectedItem is not null) BrowserList.ScrollIntoView(BrowserList.SelectedItem);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Q) { Close(); e.Handled = true; return; }
        if (ConfirmationPanel.IsVisible)
        {
            if (e.Key is Key.Enter or Key.Space or Key.Y) ConfirmLaunch();
            else if (e.Key is Key.Escape or Key.Back or Key.N) CancelLaunch();
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Enter || e.Key == Key.Space) ActivateSelection();
        else if (e.Key == Key.Back || e.Key == Key.Escape) GoBack();
        else if (e.Key == Key.Tab) OpenRoot((rootIndex + 1) % config.Roots.Count);
        else if (e.Key == Key.F2) OpenSettings();
        else if (e.Key == Key.F11) WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
    }

    private void BrowserList_DoubleTapped(object? sender, TappedEventArgs e) => ActivateSelection();
    private void OpenPrompt_Click(object sender, RoutedEventArgs e) => ActivateSelection();
    private void BackPrompt_Click(object sender, RoutedEventArgs e) => GoBack();
    private void RootsPrompt_Click(object sender, RoutedEventArgs e) => OpenRoot((rootIndex + 1) % config.Roots.Count);
    private void SettingsPrompt_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void ClosePrompt_Click(object sender, RoutedEventArgs e) => Close();
    private async void OpenSettings()
    {
        var setup = new SetupWindow(config, firstRun: false);
        if (await setup.ShowDialog<bool>(this)) { BuildRootButtons(); OpenInitialFolder(); }
        BrowserList.Focus();
    }
    private void LaunchButton_Click(object sender, RoutedEventArgs e) => ConfirmLaunch();
    private void CancelButton_Click(object sender, RoutedEventArgs e) => CancelLaunch();
    private void ConfirmLaunch()
    {
        var path = pendingLaunchPath;
        pendingLaunchPath = null;
        ConfirmationPanel.IsVisible = false;
        if (path is not null) LaunchNow(path);
    }
    private void CancelLaunch()
    {
        pendingLaunchPath = null;
        ConfirmationPanel.IsVisible = false;
        BrowserList.Focus();
    }
    private void BrowserList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void UpdateControllerStatus(object? sender, EventArgs e)
    {
        ControllerDot.Fill = new SolidColorBrush(controller?.Connected == true ? Color.FromRgb(105, 230, 195) : Color.FromRgb(105, 117, 138));
        ControllerText.Text = controller?.Connected == true ? "CONTROLLER READY" : "KEYBOARD / MOUSE";
    }

    private static IBrush? ResourceBrush(string key) => Application.Current?.Resources[key] as IBrush;
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
