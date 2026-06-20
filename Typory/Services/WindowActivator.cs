using System.Runtime.InteropServices;

namespace Typory.Services;

/// <summary>
/// Forces a window to the foreground, even when it is summoned from a process
/// that is not currently active. Windows normally refuses to let a background
/// process steal focus; the accepted workaround is to briefly attach our
/// thread's input to the thread that owns the foreground window, make the call,
/// then detach again.
/// </summary>
public static class WindowActivator
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachThreadInput(uint attach, uint attachTo, bool fAttach);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    public static void ForceToForeground(IntPtr hwnd)
    {
        var foreground = GetForegroundWindow();
        if (foreground == hwnd)
            return;

        var foregroundThread = GetWindowThreadProcessId(foreground, IntPtr.Zero);
        var thisThread = GetCurrentThreadId();

        var attached = foregroundThread != thisThread
            && AttachThreadInput(foregroundThread, thisThread, true);
        try
        {
            SetForegroundWindow(hwnd);
            BringWindowToTop(hwnd);
        }
        finally
        {
            if (attached)
                AttachThreadInput(foregroundThread, thisThread, false);
        }
    }
}
