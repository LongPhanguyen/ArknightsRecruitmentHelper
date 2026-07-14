using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RecruitmentOcrApp;

internal static class Win32Interop
{
    public const uint GA_ROOT = 2;
    public const int VK_LBUTTON = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd);
        if (length == 0) return string.Empty;

        var builder = new StringBuilder(length + 1);
        GetWindowText(hWnd, builder, builder.Capacity);
        return builder.ToString();
    }

    // The window's full bounds (including title bar/borders) in screen coordinates.
    public static RECT GetWindowScreenRect(IntPtr hWnd)
    {
        GetWindowRect(hWnd, out var rect);
        return rect;
    }

    // The window's client area (excludes title bar/borders) in screen coordinates --
    // this is what capture regions should be relative to, since that's what the
    // user actually sees as "the emulator content."
    public static RECT GetClientScreenRect(IntPtr hWnd)
    {
        GetClientRect(hWnd, out var clientRect); // Left/Top are always 0,0 here
        var topLeft = new POINT { X = 0, Y = 0 };
        ClientToScreen(hWnd, ref topLeft);

        return new RECT
        {
            Left = topLeft.X,
            Top = topLeft.Y,
            Right = topLeft.X + (clientRect.Right - clientRect.Left),
            Bottom = topLeft.Y + (clientRect.Bottom - clientRect.Top),
        };
    }
}
