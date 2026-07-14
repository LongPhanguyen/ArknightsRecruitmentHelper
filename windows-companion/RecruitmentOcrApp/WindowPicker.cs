using System;
using System.Threading;
using System.Threading.Tasks;

namespace RecruitmentOcrApp;

public sealed class PickedWindow
{
    public IntPtr Handle { get; init; }
    public string Title { get; init; } = string.Empty;
}

public static class WindowPicker
{
    private const int PollIntervalMs = 15;

    // Polls global mouse state (not a message-loop event, so this works even
    // though the click lands on some other app's window) for the next
    // left-button press anywhere on screen, then resolves the top-level
    // window under the cursor at that moment. The click also reaches
    // whatever window is under the cursor as normal -- there's no attempt to
    // suppress it, so picking the emulator window may also register as a
    // click inside the game. Acceptable for v1 per the spec.
    public static async Task<PickedWindow> WaitForClickAsync(CancellationToken cancellationToken)
    {
        // In case the click that triggered "picking mode" is still down.
        while (IsLeftButtonDown())
        {
            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsLeftButtonDown())
            {
                Win32Interop.GetCursorPos(out var point);
                var hwnd = Win32Interop.WindowFromPoint(point);

                // WindowFromPoint often returns a child control (e.g. a
                // button); walk up to the top-level window since that's what
                // we want to track and re-locate later.
                var root = Win32Interop.GetAncestor(hwnd, Win32Interop.GA_ROOT);
                if (root != IntPtr.Zero) hwnd = root;

                if (hwnd == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Could not identify a window at the clicked location.");
                }

                return new PickedWindow
                {
                    Handle = hwnd,
                    Title = Win32Interop.GetWindowTitle(hwnd),
                };
            }

            await Task.Delay(PollIntervalMs, cancellationToken);
        }
    }

    // Re-locates a previously picked window: the stored handle is cheap and
    // correct as long as the window still exists, but it goes stale if the
    // emulator process restarts, so fall back to a title-substring match.
    public static IntPtr Resolve(IntPtr lastKnownHandle, string title)
    {
        if (IsUsable(lastKnownHandle)) return lastKnownHandle;
        if (string.IsNullOrEmpty(title)) return IntPtr.Zero;

        var found = IntPtr.Zero;
        Win32Interop.EnumWindows((hWnd, _) =>
        {
            if (!Win32Interop.IsWindowVisible(hWnd)) return true;

            var candidateTitle = Win32Interop.GetWindowTitle(hWnd);
            if (!string.IsNullOrEmpty(candidateTitle) &&
                candidateTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
            {
                found = hWnd;
                return false; // stop enumerating
            }

            return true;
        }, IntPtr.Zero);

        return found;
    }

    private static bool IsUsable(IntPtr hwnd) =>
        hwnd != IntPtr.Zero && Win32Interop.IsWindow(hwnd) && Win32Interop.IsWindowVisible(hwnd);

    private static bool IsLeftButtonDown() => (Win32Interop.GetAsyncKeyState(Win32Interop.VK_LBUTTON) & 0x8000) != 0;
}
