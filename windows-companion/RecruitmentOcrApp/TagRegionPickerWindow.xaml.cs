using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RecruitmentOcrApp;

public partial class TagRegionPickerWindow : Window
{
    private Point _dragStart;
    private bool _isDragging;

    // Relative to the source bitmap's top-left, in source pixels -- callers
    // pass in the window's client-area screenshot, so this is directly the
    // offset to store for future captures.
    public Rectangle SelectedRegion { get; private set; }

    public TagRegionPickerWindow(Bitmap sourceBitmap)
    {
        InitializeComponent();

        var bitmapSource = BitmapInterop.ToBitmapSource(sourceBitmap);
        PreviewImage.Source = bitmapSource;
        PreviewImage.Width = bitmapSource.PixelWidth;
        PreviewImage.Height = bitmapSource.PixelHeight;
        SelectionCanvas.Width = bitmapSource.PixelWidth;
        SelectionCanvas.Height = bitmapSource.PixelHeight;
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(SelectionCanvas);
        _isDragging = true;

        SelectionRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRectangle, _dragStart.X);
        Canvas.SetTop(SelectionRectangle, _dragStart.Y);
        SelectionRectangle.Width = 0;
        SelectionRectangle.Height = 0;

        SelectionCanvas.CaptureMouse();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(SelectionCanvas);
        var x = Math.Min(_dragStart.X, current.X);
        var y = Math.Min(_dragStart.Y, current.Y);
        var width = Math.Abs(current.X - _dragStart.X);
        var height = Math.Abs(current.Y - _dragStart.Y);

        Canvas.SetLeft(SelectionRectangle, x);
        Canvas.SetTop(SelectionRectangle, y);
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        SelectionCanvas.ReleaseMouseCapture();

        var left = Canvas.GetLeft(SelectionRectangle);
        var top = Canvas.GetTop(SelectionRectangle);
        var width = SelectionRectangle.Width;
        var height = SelectionRectangle.Height;

        if (width < 2 || height < 2)
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
            ConfirmButton.IsEnabled = false;
            return;
        }

        SelectedRegion = new Rectangle((int)left, (int)top, (int)width, (int)height);
        ConfirmButton.IsEnabled = true;
    }

    private void OnConfirmClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
