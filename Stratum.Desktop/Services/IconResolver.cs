using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform;
using Stratum.Core;

namespace Stratum.Desktop.Services
{
    public partial class IconResolver : IIconResolver
    {
        private static HashSet<string> _keys;

        public async Task InitializeAsync()
        {
            if (_keys != null)
            {
                return;
            }

            try
            {
                using var stream = AssetLoader.Open(new Uri("avares://Stratum/Icons.csv"));
                using var reader = new StreamReader(stream);
                var keys = new HashSet<string>();
                await reader.ReadLineAsync();

                while (await reader.ReadLineAsync() is { } line)
                {
                    var key = line.Split(',')[0];

                    if (!string.IsNullOrEmpty(key))
                    {
                        keys.Add(key);
                    }
                }

                _keys = keys;
            }
            catch
            {
                _keys ??= new HashSet<string>();
            }
        }

        public IReadOnlyCollection<string> Keys => _keys ?? new HashSet<string>();

        public string FindServiceKeyByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            static string Simplify(string input)
            {
                input = input.ToLower();
                input = SimplifyRegex().Replace(input, "");
                return input.Trim();
            }

            var key = Simplify(name);

            if (_keys != null && _keys.Contains(key))
            {
                return key;
            }

            var firstWordKey = Simplify(name.Split(new[] { ' ', '.' }, 2)[0]);

            return _keys != null && _keys.Contains(firstWordKey)
                ? firstWordKey
                : null;
        }

        [GeneratedRegex("[^a-z0-9]")]
        private static partial Regex SimplifyRegex();
    }
}
