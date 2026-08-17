using Avalonia.Threading;
using SDL3;

namespace PadPath.Input;

public enum GamepadAction { Up, Down, Left, Right, Accept, Back, Roots, Settings, Quit }

public sealed class GamepadController : IDisposable
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(45) };
    private readonly Dictionary<SDL.GamepadButton, DateTime> heldSince = [];
    private readonly Dictionary<SDL.GamepadButton, DateTime> lastRepeat = [];
    private readonly HashSet<SDL.GamepadButton> previous = [];
    private nint gamepad;
    private bool initialized;

    public event Action<GamepadAction>? Pressed;
    public bool Connected => gamepad != 0 && SDL.GamepadConnected(gamepad);

    public GamepadController()
    {
        try
        {
            initialized = SDL.Init(SDL.InitFlags.Gamepad);
            timer.Tick += Poll;
            timer.Start();
        }
        catch (Exception) when (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsWindows())
        {
            initialized = false;
        }
    }

    private void Poll(object? sender, EventArgs e)
    {
        if (!initialized) return;
        SDL.UpdateGamepads();
        EnsureGamepad();
        if (!Connected) { previous.Clear(); heldSince.Clear(); return; }
        Map(SDL.GamepadButton.DPadUp, GamepadAction.Up, true);
        Map(SDL.GamepadButton.DPadDown, GamepadAction.Down, true);
        Map(SDL.GamepadButton.DPadLeft, GamepadAction.Left, true);
        Map(SDL.GamepadButton.DPadRight, GamepadAction.Right, true);
        Map(SDL.GamepadButton.South, GamepadAction.Accept);
        Map(SDL.GamepadButton.East, GamepadAction.Back);
        Map(SDL.GamepadButton.North, GamepadAction.Settings);
        Map(SDL.GamepadButton.LeftShoulder, GamepadAction.Roots);
        Map(SDL.GamepadButton.Start, GamepadAction.Quit);
    }

    private void EnsureGamepad()
    {
        if (Connected) return;
        if (gamepad != 0) { SDL.CloseGamepad(gamepad); gamepad = 0; }
        var ids = SDL.GetGamepads(out _);
        if (ids is { Length: > 0 }) gamepad = SDL.OpenGamepad(ids[0]);
    }

    private void Map(SDL.GamepadButton button, GamepadAction action, bool repeat = false)
    {
        var down = SDL.GetGamepadButton(gamepad, button);
        var wasDown = previous.Contains(button);
        var now = DateTime.UtcNow;
        if (down && !wasDown)
        {
            previous.Add(button); heldSince[button] = now; lastRepeat[button] = now; Pressed?.Invoke(action);
        }
        else if (down && repeat && heldSince.TryGetValue(button, out var held) && now - held > TimeSpan.FromMilliseconds(360)
                 && now - lastRepeat.GetValueOrDefault(button) > TimeSpan.FromMilliseconds(105))
        {
            lastRepeat[button] = now; Pressed?.Invoke(action);
        }
        else if (!down) { previous.Remove(button); heldSince.Remove(button); }
    }

    public void Dispose()
    {
        timer.Stop();
        if (gamepad != 0) SDL.CloseGamepad(gamepad);
        if (initialized) SDL.QuitSubSystem(SDL.InitFlags.Gamepad);
    }
}
