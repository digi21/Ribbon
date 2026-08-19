using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace RibbonGallery;

// Opens the gallery, lets it settle, saves a picture of it and exits. Run it with:
//
//     dotnet run --project samples/RibbonGallery -- --screenshot gallery.png [light|dark]
//
// It is how the pictures in the README are produced - one of each theme, because GitHub renders a
// README in the theme of whoever is reading it and a picture that ignores that reads as a hole in
// the page. It is also a quick way to see that a layout change has not collapsed something.
internal sealed class ScreenshotRun(string outputPath, ElementTheme theme)
{
    private MainWindow? window;
    private int frame;

    internal void Run()
    {
        window = new MainWindow();

        // Through the gallery's own selector rather than straight onto the root, so that the picture
        // does not show a window in one theme with a box saying it is in another.
        window.SetThemeForPicture(theme);

        // Wide enough for every group at its largest, and short enough that the ribbon is the
        // subject rather than a strip above a page of prose.
        window.AppWindow.Resize(new SizeInt32(1240, 620));
        window.Activate();

        CompositionTarget.Rendering += OnRendering;
    }

    private static async Task CaptureAsync(FrameworkElement element, string path)
    {
        RenderTargetBitmap bitmap = new();
        await bitmap.RenderAsync(element);

        IBuffer pixels = await bitmap.GetPixelsAsync();

        using InMemoryRandomAccessStream stream = new();
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth,
            (uint)bitmap.PixelHeight,
            96,
            96,
            pixels.ToArray());
        await encoder.FlushAsync();

        using Stream managed = stream.AsStreamForRead();
        using FileStream file = File.Create(path);
        await managed.CopyToAsync(file);
    }

    private async void OnRendering(object? sender, object e)
    {
        frame++;

        if (frame < 20 || window?.Content is not FrameworkElement content)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;

        try
        {
            await CaptureAsync(content, outputPath);
        }
        finally
        {
            window.Close();
            Application.Current.Exit();
        }
    }
}
