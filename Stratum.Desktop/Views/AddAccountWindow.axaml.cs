using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Core.Service;
using Stratum.Desktop.Services;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop.Views
{
    public partial class AddAccountWindow : Window
    {
        public AddAccountWindow(IAuthenticatorService authenticatorService, ICategoryService categoryService,
            IIconResolver iconResolver, IImportService importService, IRestoreService restoreService,
            Authenticator existing)
        {
            InitializeComponent();
            _ = InitializeAsync(authenticatorService, categoryService, iconResolver, importService, restoreService,
                existing);
        }

        private async Task InitializeAsync(IAuthenticatorService authenticatorService,
            ICategoryService categoryService, IIconResolver iconResolver, IImportService importService,
            IRestoreService restoreService, Authenticator existing)
        {
            var viewModel = new AddAccountViewModel(authenticatorService, categoryService, iconResolver,
                importService, restoreService, existing);
            await viewModel.InitializeAsync();
            DataContext = viewModel;
            Title = viewModel.IsEdit ? AppStrings.EditTitle : AppStrings.AddTitle;
            AddButton.Content = viewModel.IsEdit ? AppStrings.SaveButton : AppStrings.AddButton;
            viewModel.RequestClose += Close;
            HeaderBar.TitleText = viewModel.IsEdit ? AppStrings.EditTitle : AppStrings.AddTitle;
        }

        private async void OnScanQrClicked(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择二维码图片",
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

            var text = QrScanner.Decode(data);

            if (DataContext is AddAccountViewModel viewModel)
            {
                viewModel.SetScannedUri(text);
            }
        }

        private async void OnSelectImportFileClicked(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = AppStrings.SelectBackupFile,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(AppStrings.AllFiles) { Patterns = new[] { "*.*" } }
                }
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

            if (DataContext is AddAccountViewModel viewModel)
            {
                viewModel.SetImportData(data, files[0].Name);
            }
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
