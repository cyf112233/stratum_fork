using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Stratum.Desktop.Services
{
    public static class AppIcons
    {
        private static WindowIcon _windowIcon;

        public static WindowIcon GetWindowIcon()
        {
            if (_windowIcon == null)
            {
                using var stream = AssetLoader.Open(new Uri("avares://Stratum/Resources/icon.png"));
                _windowIcon = new WindowIcon(stream);
            }

            return _windowIcon;
        }
    }
}
