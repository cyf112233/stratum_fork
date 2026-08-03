using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Stratum.Desktop.Services
{
    public static class IconLoader
    {
        private const string BaseUri = "avares://Stratum/Icons/";

        public static bool IsDarkTheme()
        {
            return Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        }

        private static bool Exists(string name)
        {
            return AssetLoader.Exists(new Uri(BaseUri + name));
        }

        public static Bitmap LoadOrDefault(string key, bool isDark)
        {
            string candidate = null;

            if (!string.IsNullOrEmpty(key))
            {
                if (isDark)
                {
                    if (Exists(key + "_dark.png"))
                    {
                        candidate = BaseUri + key + "_dark.png";
                    }
                    else if (Exists(key + ".png"))
                    {
                        candidate = BaseUri + key + ".png";
                    }
                }
                else
                {
                    if (Exists(key + ".png"))
                    {
                        candidate = BaseUri + key + ".png";
                    }
                    else if (Exists(key + "_dark.png"))
                    {
                        candidate = BaseUri + key + "_dark.png";
                    }
                }
            }

            if (candidate == null)
            {
                if (isDark)
                {
                    if (Exists("default_dark.png"))
                    {
                        candidate = BaseUri + "default_dark.png";
                    }
                    else if (Exists("default.png"))
                    {
                        candidate = BaseUri + "default.png";
                    }
                }
                else
                {
                    if (Exists("default.png"))
                    {
                        candidate = BaseUri + "default.png";
                    }
                    else if (Exists("default_dark.png"))
                    {
                        candidate = BaseUri + "default_dark.png";
                    }
                }
            }

            if (candidate == null)
            {
                return null;
            }

            try
            {
                using var stream = AssetLoader.Open(new Uri(candidate));
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}
