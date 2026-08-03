using System;
using System.IO;
using System.Text.Json;

namespace Stratum.Desktop.Services
{
    public class AppSettings
    {
        public string Language { get; set; } = "Auto";
        public string Theme { get; set; } = "Auto";
        public bool ClickToCopy { get; set; } = true;
        public bool HideCodes { get; set; } = false;
        public bool ConfirmDeletes { get; set; } = true;
    }

    public class SettingsService
    {
        private readonly string _path;

        public SettingsService(string path)
        {
            _path = path;
            Load();
        }

        public AppSettings Settings { get; private set; } = new();

        private void Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    Settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
                }
            }
            catch
            {
                Settings = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_path, JsonSerializer.Serialize(Settings));
            }
            catch
            {
            }
        }
    }
}
