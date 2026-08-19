using Microsoft.UI.Xaml;

namespace RibbonGallery;

public partial class App : Application
{
    private Window? window;
    private ScreenshotRun? screenshot;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // `--screenshot <path> [light|dark]` opens the gallery, takes its picture and exits. See
        // ScreenshotRun.cs.
        string[] arguments = Environment.GetCommandLineArgs();

        int flag = Array.IndexOf(arguments, "--screenshot");
        if (flag >= 0 && flag + 1 < arguments.Length)
        {
            ElementTheme theme = arguments.Contains("dark")
                ? ElementTheme.Dark
                : arguments.Contains("light") ? ElementTheme.Light : ElementTheme.Default;

            screenshot = new ScreenshotRun(arguments[flag + 1], theme);
            screenshot.Run();
            return;
        }

        window = new MainWindow();
        window.Activate();
    }
}
