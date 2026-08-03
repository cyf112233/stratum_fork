using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Core.Util;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Views
{
    public partial class ChangeIconWindow : Window
    {
        private sealed record IconEntry(string Key, Bitmap Bitmap);

        private readonly IIconResolver _iconResolver;
        private readonly IIconPackRepository _iconPackRepository;
        private readonly IIconPackEntryRepository _iconPackEntryRepository;
        private readonly ICustomIconService _customIconService;
        private List<IconEntry> _entries = new();
        private string _result;

        public ChangeIconWindow(IIconResolver iconResolver, IIconPackRepository iconPackRepository,
            IIconPackEntryRepository iconPackEntryRepository, ICustomIconService customIconService)
        {
            InitializeComponent();
            Icon = AppIcons.GetWindowIcon();
            _iconResolver = iconResolver;
            _iconPackRepository = iconPackRepository;
            _iconPackEntryRepository = iconPackEntryRepository;
            _customIconService = customIconService;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            _entries = new List<IconEntry>();

            if (_iconResolver is IconResolver resolver)
            {
                foreach (var key in resolver.Keys)
                {
                    var bitmap = IconLoader.LoadOrDefault(key, IconLoader.IsDarkTheme());

                    if (bitmap != null)
                    {
                        _entries.Add(new IconEntry(key, bitmap));
                    }
                }
            }

            var packs = await _iconPackRepository.GetAllAsync();

            foreach (var pack in packs)
            {
                var entries = await _iconPackEntryRepository.GetAllForPackAsync(pack);

                foreach (var entry in entries)
                {
                    try
                    {
                        using var memory = new MemoryStream(entry.Data);
                        var bitmap = new Bitmap(memory);
                        _entries.Add(new IconEntry(CustomIcon.Prefix + entry.Name, bitmap));
                    }
                    catch
                    {
                    }
                }
            }

            ApplyFilter();

            SearchBox.TextChanged += (_, _) => ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = SearchBox.Text?.Trim() ?? "";

            IconsGrid.ItemsSource = string.IsNullOrEmpty(query)
                ? _entries
                : _entries.Where(e => e.Key.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public string GetResult()
        {
            return _result;
        }

        private void OnIconClicked(object sender, PointerPressedEventArgs e)
        {
            if ((e.Source as Border)?.Tag is string key)
            {
                _result = key;
                Close();
            }
        }

        private async void OnUploadCustomIconClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = AppStrings.SelectImage,
                    AllowMultiple = false,
                    FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
                });

                if (files.Count == 0)
                {
                    return;
                }

                byte[] data;

                await using (var stream = await files[0].OpenReadAsync())
                using (var memory = new MemoryStream())
                {
                    await stream.CopyToAsync(memory);
                    data = memory.ToArray();
                }

                if (data.Length == 0)
                {
                    return;
                }

                var id = HashUtil.Sha1(Convert.ToBase64String(data));
                var icon = new CustomIcon { Id = id, Data = data };
                await _customIconService.AddIfNotExistsAsync(icon);
                _result = CustomIcon.Prefix + id;
                Close();
            }
            catch
            {
            }
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
