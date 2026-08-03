using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Fonts;
using Stratum.Desktop.Services;

namespace Stratum.Desktop;

class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stratum");
        SingleInstance.Initialize(dataDir);

        if (!SingleInstance.Acquire())
        {
            return 0;
        }

        var result = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        SingleInstance.Release();
        return result;
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .ConfigureFonts(fontManager =>
            {
                try
                {
                    fontManager.AddFontCollection(new EmbeddedFontCollection(
                        new Uri("fonts:StratumFonts", UriKind.Absolute),
                        new Uri("avares://Stratum/Fonts", UriKind.Absolute)));
                }
                catch
                {
                }
            })
            .LogToTrace();
}
