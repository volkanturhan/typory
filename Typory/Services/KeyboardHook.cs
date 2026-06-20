using System.Runtime.InteropServices;
using System.Text;

namespace Typory.Services;

/// <summary>
/// A system-wide low-level keyboard hook that watches what the user types and
/// keeps a small rolling buffer of the most recent characters. After every typed
/// character it raises <see cref="Typed"/> with the current buffer, so the app
/// can check whether it now ends with one of the configured abbreviations.
///
/// The buffer mirrors "the run of text typed so far": Backspace trims it, and
/// anything that moves the caret or starts a shortcut (Enter, Tab, arrows,
/// Ctrl/Alt/Win combos…) clears it. Characters are decoded with the foreground
/// window's keyboard layout so non-US layouts (e.g. Turkish) expand correctly.
///
/// The hook never swallows keystrokes — it only observes — and ignores the
/// synthetic input produced by <see cref="TextInjector"/>.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private const int VK_BACK = 0x08;
    private const int VK_TAB = 0x09;
    private const int VK_RETURN = 0x0D;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt
    private const int VK_CAPITAL = 0x14;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_PRIOR = 0x21; // Page Up
    private const int VK_NEXT = 0x22;  // Page Down
    private const int VK_END = 0x23;
    private const int VK_HOME = 0x24;
    private const int VK_LEFT = 0x25;
    private const int VK_UP = 0x26;
    private const int VK_RIGHT = 0x27;
    private const int VK_DOWN = 0x28;
    private const int VK_DELETE = 0x2E;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    // The left/right-specific modifier codes, which is what a low-level hook
    // actually receives (e.g. Left Ctrl is 0xA2, not the generic 0x11).
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_LMENU = 0xA4;
    private const int VK_RMENU = 0xA5;

    private const int MaxBufferLength = 64;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    // Keep the delegate alive for the hook's lifetime so it is not collected.
    private readonly HookProc _proc;
    private readonly IntPtr _hookId;
    private readonly StringBuilder _buffer = new();
    private bool _disposed;

    /// <summary>When true the hook still observes but does not track typing.</summary>
    public bool Paused { get; set; }

    /// <summary>Raised after each typed character with the current rolling buffer.</summary>
    public event Action<string>? Typed;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (_hookId == IntPtr.Zero)
            throw new InvalidOperationException("Could not install the keyboard hook.");
    }

    /// <summary>Clears the rolling buffer (e.g. right after an expansion).</summary>
    public void ResetBuffer() => _buffer.Clear();

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && !Paused)
        {
            var message = wParam.ToInt32();
            if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                // Skip the keystrokes we synthesise ourselves.
                if (data.dwExtraInfo != TextInjector.InjectedSignature)
                    HandleKeyDown((int)data.vkCode, data.scanCode);
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void HandleKeyDown(int vk, uint scanCode)
    {
        // Modifier keys on their own change nothing. This must come before the
        // shortcut check below: AltGr arrives as a Ctrl-down immediately followed
        // by an Alt-down, and we must not treat that lone Ctrl-down as a shortcut
        // (which would wrongly clear the buffer mid-word).
        switch (vk)
        {
            case VK_SHIFT:
            case VK_CONTROL:
            case VK_MENU:
            case VK_CAPITAL:
            case VK_LWIN:
            case VK_RWIN:
            case VK_LSHIFT:
            case VK_RSHIFT:
            case VK_LCONTROL:
            case VK_RCONTROL:
            case VK_LMENU:
            case VK_RMENU:
                return;
        }

        var ctrl = IsDown(VK_CONTROL);
        var alt = IsDown(VK_MENU);
        var win = IsDown(VK_LWIN) || IsDown(VK_RWIN);

        // A real shortcut (Win+key, Ctrl+key, or Alt+key) is a command, not
        // typing. Ctrl+Alt is left alone because that is AltGr, which produces
        // text on many layouts.
        if (win || (ctrl && !alt) || (alt && !ctrl))
        {
            _buffer.Clear();
            return;
        }

        switch (vk)
        {
            case VK_BACK:
                if (_buffer.Length > 0)
                    _buffer.Remove(_buffer.Length - 1, 1);
                return;

            // Keys that move the caret or end the current word: the buffer no
            // longer reflects the text just before the cursor, so drop it.
            case VK_RETURN:
            case VK_TAB:
            case VK_ESCAPE:
            case VK_DELETE:
            case VK_LEFT:
            case VK_UP:
            case VK_RIGHT:
            case VK_DOWN:
            case VK_HOME:
            case VK_END:
            case VK_PRIOR:
            case VK_NEXT:
                _buffer.Clear();
                return;
        }

        var text = Translate((uint)vk, scanCode);
        if (text.Length == 0)
            return;

        foreach (var ch in text)
        {
            // Ignore control characters; only real text builds the buffer.
            if (ch >= ' ')
                _buffer.Append(ch);
        }

        if (_buffer.Length > MaxBufferLength)
            _buffer.Remove(0, _buffer.Length - MaxBufferLength);

        Typed?.Invoke(_buffer.ToString());
    }

    // Decode a key to its character(s) using the foreground app's keyboard
    // layout, honouring Shift and Caps Lock. Returns "" for dead keys or keys
    // with no character.
    private static string Translate(uint vk, uint scanCode)
    {
        var keyState = new byte[256];
        GetKeyboardState(keyState);

        // GetKeyboardState can lag inside a low-level hook, so pin the keys that
        // decide the character from the live state.
        keyState[VK_SHIFT] = (byte)(IsDown(VK_SHIFT) ? 0x80 : 0);
        keyState[VK_CAPITAL] = (byte)(GetKeyState(VK_CAPITAL) & 0x0001);

        var layout = GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero));

        var buffer = new StringBuilder(8);
        var result = ToUnicodeEx(vk, scanCode, keyState, buffer, buffer.Capacity, 0, layout);
        return result > 0 ? buffer.ToString(0, result) : string.Empty;
    }

    private static bool IsDown(int vk) => (GetKeyState(vk) & 0x8000) != 0;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_hookId != IntPtr.Zero)
            UnhookWindowsHookEx(_hookId);
    }
}
