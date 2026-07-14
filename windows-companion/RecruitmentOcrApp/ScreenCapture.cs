using System.Drawing;
using System.Drawing.Imaging;

namespace RecruitmentOcrApp;

public static class ScreenCapture
{
    // Grabs a fixed screen-pixel rectangle via GDI BitBlt. Assumes the emulator
    // window is positioned/sized consistently so this rectangle always lands on
    // the recruitment tag row.
    public static Bitmap CaptureRegion(Rectangle region)
    {
        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }
}
