using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PadPath.Input;
using PadPath.Models;
using PadPath.Services;

namespace PadPath;

public partial class MainWindow : Window
{
    private readonly LauncherConfig config;
    private readonly FileBrowserService browser;
    private readonly XInputController? controller;
    private readonly ObservableCollection<BrowserItem> items = [];
    private RootConfig activeRoot = null!;
    private string currentDirectory = "";
    private string? pendingLaunchPath;
    private bool gameIsRunning;
    private int rootIndex;

    public MainWindow(LauncherConfig config)
    {
        this.config = config;
        browser = new FileBrowserService(config);
        if (!string.Equals(Environment.GetEnvironmentVariable("HANDHELD_LAUNCHER_DISABLE_CONTROLLER"), "1", StringComparison.Ordinal))
            controller = new XInputController();
        InitializeComponent();
        TitleText.Text = config.Title;
        BrowserList.ItemsSource = items;
        BuildRootButtons();
        if (controller is not null) controller.Pressed += HandleGamepad;
        Closed += (_, _) => controller?.Dispose();
        Loaded += (_, _) =>
        {
            if (config.Fullscreen) { WindowStyle = WindowStyle.None; WindowState = WindowState.Maximized; }
            OpenInitialFolder();
            BrowserList.Focus();
        };
        CompositionTarget.Rendering += UpdateControllerStatus;
        StateChanged += (_, _) => { if (gameIsRunning && WindowState != WindowState.Minimized) WindowState = WindowState.Minimized; };
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
            var button = new Button { Content = config.Roots[i].Name, Style = (Style)FindResource("RootButtonStyle"), Tag = i };
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
            MessageBox.Show($"The configured folder does not exist:\n{rootPath}", activeRoot.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
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
        EmptyPanel.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (items.Count > 0) BrowserList.SelectedIndex = 0;
        if (config.RememberLastFolder) ConfigService.SaveLastFolder(destination);
        UpdateRootButtonState();
    }

    private void UpdateRootButtonState()
    {
        foreach (Button button in RootButtons.Children)
        {
            var active = (int)button.Tag == rootIndex;
            button.BorderBrush = (Brush)FindResource("BorderBrush");
            button.Background = active ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("PanelBrush");
            button.Foreground = active ? (Brush)FindResource("AccentTextBrush") : (Brush)FindResource("TextBrush");
        }
    }

    private void ActivateSelection()
    {
        if (BrowserList.SelectedItem is not BrowserItem item) return;
        if (item.IsDirectory) Navigate(item.FullPath);
        else Launch(item.FullPath);
    }

    private void Launch(string path)
    {
        if (config.ConfirmBeforeLaunch)
        {
            pendingLaunchPath = path;
            ConfirmationName.Text = Path.GetFileNameWithoutExtension(path);
            ConfirmationPanel.Visibility = Visibility.Visible;
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
            gameIsRunning = true;
            WindowState = WindowState.Minimized;

            var grace = Task.Delay(TimeSpan.FromSeconds(Math.Clamp(config.MinimumHandoffSeconds, 5, 120)));
            try { await Task.WhenAll(target.WaitForExitAsync(), grace); }
            catch (InvalidOperationException) { await grace; }

            gameIsRunning = false;
            WindowState = WindowState.Maximized;
            Activate();
            BrowserList.Focus();
        }
        catch (Exception ex) { gameIsRunning = false; WindowState = WindowState.Maximized; MessageBox.Show(ex.Message, "Launch failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void GoBack()
    {
        var parent = Directory.GetParent(currentDirectory)?.FullName;
        var rootPath = Environment.ExpandEnvironmentVariables(activeRoot.Path);
        if (parent is not null && FileBrowserService.IsWithinRoot(parent, rootPath)) Navigate(parent);
    }

    private void HandleGamepad(GamepadAction action) => Dispatcher.Invoke(() =>
    {
        if (action == GamepadAction.Quit) { Close(); return; }
        if (ConfirmationPanel.Visibility == Visibility.Visible)
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
        BrowserList.ScrollIntoView(BrowserList.SelectedItem);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Q) { Close(); e.Handled = true; return; }
        if (ConfirmationPanel.Visibility == Visibility.Visible)
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
        else if (e.Key == Key.F11) { WindowStyle = WindowStyle == WindowStyle.None ? WindowStyle.SingleBorderWindow : WindowStyle.None; WindowState = WindowState.Maximized; }
    }

    private void BrowserList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => ActivateSelection();
    private void OpenPrompt_Click(object sender, RoutedEventArgs e) => ActivateSelection();
    private void BackPrompt_Click(object sender, RoutedEventArgs e) => GoBack();
    private void RootsPrompt_Click(object sender, RoutedEventArgs e) => OpenRoot((rootIndex + 1) % config.Roots.Count);
    private void SettingsPrompt_Click(object sender, RoutedEventArgs e) => OpenSettings();
    private void ClosePrompt_Click(object sender, RoutedEventArgs e) => Close();
    private void OpenSettings()
    {
        var setup = new SetupWindow(config, firstRun: false) { Owner = this };
        if (setup.ShowDialog() == true) { BuildRootButtons(); OpenInitialFolder(); }
        BrowserList.Focus();
    }
    private void LaunchButton_Click(object sender, RoutedEventArgs e) => ConfirmLaunch();
    private void CancelButton_Click(object sender, RoutedEventArgs e) => CancelLaunch();
    private void ConfirmLaunch()
    {
        var path = pendingLaunchPath;
        pendingLaunchPath = null;
        ConfirmationPanel.Visibility = Visibility.Collapsed;
        if (path is not null) LaunchNow(path);
    }
    private void CancelLaunch()
    {
        pendingLaunchPath = null;
        ConfirmationPanel.Visibility = Visibility.Collapsed;
        BrowserList.Focus();
    }
    private void BrowserList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void UpdateControllerStatus(object? sender, EventArgs e)
    {
        ControllerDot.Fill = new SolidColorBrush(controller?.Connected == true ? Color.FromRgb(105, 230, 195) : Color.FromRgb(105, 117, 138));
        ControllerText.Text = controller?.Connected == true ? "CONTROLLER READY" : "KEYBOARD / MOUSE";
    }
}
