using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace HandheldLauncher.Input;

public enum GamepadAction { Up, Down, Left, Right, Accept, Back, Roots, Settings, Quit }

public sealed class XInputController : IDisposable
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(45) };
    private readonly nint stateBuffer = Marshal.AllocHGlobal(16);
    private ushort previous;
    private readonly Dictionary<ushort, DateTime> heldSince = [];
    private readonly Dictionary<ushort, DateTime> lastRepeat = [];
    public event Action<GamepadAction>? Pressed;
    public bool Connected { get; private set; }

    public XInputController()
    {
        timer.Tick += Poll;
        timer.Start();
    }

    private void Poll(object? sender, EventArgs e)
    {
        try { Connected = XInputGetState(0, stateBuffer) == 0; }
        catch (DllNotFoundException) { Connected = false; timer.Stop(); }
        catch (EntryPointNotFoundException) { Connected = false; timer.Stop(); }
        if (!Connected) { previous = 0; heldSince.Clear(); return; }
        var buttons = unchecked((ushort)Marshal.ReadInt16(stateBuffer, 4));
        Map(buttons, 0x0001, GamepadAction.Up, true);
        Map(buttons, 0x0002, GamepadAction.Down, true);
        Map(buttons, 0x0004, GamepadAction.Left, true);
        Map(buttons, 0x0008, GamepadAction.Right, true);
        Map(buttons, 0x1000, GamepadAction.Accept);
        Map(buttons, 0x2000, GamepadAction.Back);
        Map(buttons, 0x8000, GamepadAction.Settings);
        Map(buttons, 0x0020, GamepadAction.Roots);
        Map(buttons, 0x0040, GamepadAction.Quit);
        previous = buttons;
    }

    private void Map(ushort buttons, ushort mask, GamepadAction action, bool repeat = false)
    {
        var down = (buttons & mask) != 0;
        var wasDown = (previous & mask) != 0;
        var now = DateTime.UtcNow;
        if (down && !wasDown)
        {
            heldSince[mask] = now; lastRepeat[mask] = now; Pressed?.Invoke(action);
        }
        else if (down && repeat && heldSince.TryGetValue(mask, out var held) && now - held > TimeSpan.FromMilliseconds(360)
                 && now - lastRepeat.GetValueOrDefault(mask) > TimeSpan.FromMilliseconds(105))
        {
            lastRepeat[mask] = now; Pressed?.Invoke(action);
        }
        else if (!down) heldSince.Remove(mask);
    }

    public void Dispose() { timer.Stop(); Marshal.FreeHGlobal(stateBuffer); }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetState(uint userIndex, nint state);
}
