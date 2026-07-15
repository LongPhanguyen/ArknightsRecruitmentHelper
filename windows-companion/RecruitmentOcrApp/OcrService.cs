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

public sealed class OcrWordResult
{
    public string Text { get; }
    public Rectangle BoundingRect { get; }

    public OcrWordResult(string text, Rectangle boundingRect)
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

    // Every recognized word, in reading order, with its own bounding box.
    // Used by TagRegionDetector, which slides a window across this flattened
    // sequence rather than matching per-line -- a hyphenated tag like
    // "DP-Recovery" can get OCR'd as two separate lines ("DP", "Recovery"),
    // and per-line matching would silently miss it since neither line alone
    // contains the full tag name. Flattening to words keeps them adjacent
    // regardless of which line OCR grouped them into.
    public async Task<IReadOnlyList<OcrWordResult>> RecognizeWordsAsync(Bitmap bitmap)
    {
        var result = await RunAsync(bitmap);
        return result.Lines
            .SelectMany(line => line.Words)
            .Select(word => new OcrWordResult(word.Text, ToRectangle(word.BoundingRect)))
            .ToList();
    }

    private async Task<OcrResult> RunAsync(Bitmap bitmap)
    {
        using var softwareBitmap = await ToSoftwareBitmapAsync(bitmap);
        return await _engine.RecognizeAsync(softwareBitmap);
    }

    private static Rectangle ToRectangle(Windows.Foundation.Rect rect) =>
        Rectangle.FromLTRB(
            (int)rect.X,
            (int)rect.Y,
            (int)Math.Ceiling(rect.X + rect.Width),
            (int)Math.Ceiling(rect.Y + rect.Height));

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
