using System.Drawing;
using System.Drawing.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace RecruitmentOcrApp;

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
        using var softwareBitmap = await ToSoftwareBitmapAsync(bitmap);
        var result = await _engine.RecognizeAsync(softwareBitmap);
        return result.Text;
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
