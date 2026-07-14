using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace RecruitmentOcrApp;

public sealed class OcrLineResult
{
    public string Text { get; }
    public Rectangle BoundingRect { get; }

    public OcrLineResult(string text, Rectangle boundingRect)
    {
        Text = text;
        BoundingRect = boundingRect;
    }
}

public sealed class OcrService
{
    private readonly OcrEngine _engine;

    public OcrService()
    {
        _engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new InvalidOperationException(
                "No OCR language pack installed. Add one via Settings > Time & Language > " +
                "Language & region > Add a language, making sure to include the " +
                "'Optical character recognition' component.");
    }

    public async Task<string> RecognizeAsync(Bitmap bitmap)
    {
        var result = await RunAsync(bitmap);
        return result.Text;
    }

    // Per-line text plus bounding box (in the source bitmap's pixel space),
    // used by TagRegionDetector to locate the tag row without needing to
    // know any in-game label text.
    public async Task<IReadOnlyList<OcrLineResult>> RecognizeLinesAsync(Bitmap bitmap)
    {
        var result = await RunAsync(bitmap);
        return result.Lines
            .Select(line => new OcrLineResult(line.Text, UnionOf(line.Words)))
            .ToList();
    }

    private async Task<OcrResult> RunAsync(Bitmap bitmap)
    {
        using var softwareBitmap = await ToSoftwareBitmapAsync(bitmap);
        return await _engine.RecognizeAsync(softwareBitmap);
    }

    // OcrLine has no bounding rect of its own -- only its constituent words
    // do -- so build the line's rect as the union of its words' rects.
    private static Rectangle UnionOf(IReadOnlyList<OcrWord> words)
    {
        var left = words.Min(w => w.BoundingRect.X);
        var top = words.Min(w => w.BoundingRect.Y);
        var right = words.Max(w => w.BoundingRect.X + w.BoundingRect.Width);
        var bottom = words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height);
        return Rectangle.FromLTRB((int)left, (int)top, (int)Math.Ceiling(right), (int)Math.Ceiling(bottom));
    }

    // Round-trips through an in-memory PNG so SoftwareBitmap decoding handles the
    // pixel format conversion for us instead of hand-rolling a BGRA8 buffer copy.
    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, ImageFormat.Png);
        pngStream.Position = 0;

        using var randomAccessStream = new InMemoryRandomAccessStream();
        var writer = new DataWriter(randomAccessStream);
        writer.WriteBytes(pngStream.ToArray());
        await writer.StoreAsync();
        await writer.FlushAsync();
        writer.DetachStream(); // otherwise disposing the writer closes randomAccessStream too
        writer.Dispose();
        randomAccessStream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}
