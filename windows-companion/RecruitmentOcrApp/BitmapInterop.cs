using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace RecruitmentOcrApp;

public static class BitmapInterop
{
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    // CreateBitmapSourceFromHBitmap reports the result at 96 DPI regardless
    // of the source, which is exactly what we want: it means 1 WPF layout
    // unit == 1 source pixel, so an Image displayed at Stretch="None" maps
    // mouse coordinates directly onto the captured screenshot's pixels with
    // no separate scale-factor math needed.
    public static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bitmapSource.Freeze();
            return bitmapSource;
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }
}
