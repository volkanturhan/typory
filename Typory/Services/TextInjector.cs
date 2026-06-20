using System.Runtime.InteropServices;

namespace Typory.Services;

/// <summary>
/// Replaces a just-typed abbreviation with its expansion by synthesising
/// keystrokes: first a run of Backspaces to delete the abbreviation, then the
/// expansion typed out as Unicode characters (with Enter for line breaks).
///
/// Every synthetic event is tagged with <see cref="InjectedSignature"/> in its
/// <c>dwExtraInfo</c> so the keyboard hook can recognise our own input and
/// ignore it instead of treating it as more typing.
/// </summary>
public static class TextInjector
{
    /// <summary>Marker placed on injected key events so the hook skips them.</summary>
    public static readonly IntPtr InjectedSignature = new(0x54_79_70_79); // 'Typy'

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const ushort VK_BACK = 0x08;
    private const ushort VK_RETURN = 0x0D;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// Deletes <paramref name="backspaces"/> characters, then types
    /// <paramref name="text"/>.
    /// </summary>
    public static void Replace(int backspaces, string text)
    {
        var inputs = new List<INPUT>(backspaces * 2 + text.Length * 2);

        for (var i = 0; i < backspaces; i++)
        {
            inputs.Add(KeyDown(VK_BACK));
            inputs.Add(KeyUp(VK_BACK));
        }

        foreach (var ch in text)
        {
            if (ch == '\r')
                continue; // collapse CRLF to a single Enter via the '\n' below

            if (ch == '\n')
            {
                inputs.Add(KeyDown(VK_RETURN));
                inputs.Add(KeyUp(VK_RETURN));
            }
            else
            {
                inputs.Add(UnicodeDown(ch));
                inputs.Add(UnicodeUp(ch));
            }
        }

        if (inputs.Count > 0)
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    private static INPUT KeyDown(ushort vk) => Key(vk, '\0', 0);
    private static INPUT KeyUp(ushort vk) => Key(vk, '\0', KEYEVENTF_KEYUP);
    private static INPUT UnicodeDown(char ch) => Key(0, ch, KEYEVENTF_UNICODE);
    private static INPUT UnicodeUp(char ch) => Key(0, ch, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP);

    private static INPUT Key(ushort vk, char scan, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = scan,
                dwFlags = flags,
                time = 0,
                dwExtraInfo = InjectedSignature,
            },
        },
    };
}
