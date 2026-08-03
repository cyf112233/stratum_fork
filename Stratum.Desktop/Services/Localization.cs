using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Stratum.Desktop.Services
{
    public static class Localization
    {
        public static string Resolve(string setting)
        {
            return setting switch
            {
                "zh" => "zh",
                "en" => "en",
                _ => CultureInfo.CurrentUICulture.Name.StartsWith("zh") ? "zh" : "en"
            };
        }

        public static void Apply(string code)
        {
            AppStrings.SetLanguage(code);
            var app = Application.Current;

            if (app == null)
            {
                return;
            }

            var toRemove = new List<string>();

            foreach (var key in app.Resources.Keys)
            {
                if (key is string s && s.StartsWith("Str."))
                {
                    toRemove.Add(s);
                }
            }

            foreach (var key in toRemove)
            {
                app.Resources.Remove(key);
            }

            var uri = new Uri($"avares://Stratum/Strings/strings.{code}.axaml");

            if (AvaloniaXamlLoader.Load(uri) is ResourceDictionary dictionary)
            {
                foreach (var pair in dictionary)
                {
                    app.Resources[pair.Key] = pair.Value;
                }
            }
        }
    }
}
