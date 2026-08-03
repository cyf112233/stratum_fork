using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Stratum.Core.Comparer;
using Stratum.Core.Service.Impl;
using Stratum.Desktop.ViewModels;
using Stratum.Desktop.Views;

namespace Stratum.Desktop
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                SQLitePCL.Batteries_V2.Init();
                desktop.MainWindow = new MainWindow();
                Services.SingleInstance.Activated += () =>
                {
                    if (desktop.MainWindow is MainWindow window)
                    {
                        window.Show();
                        window.WindowState = Avalonia.Controls.WindowState.Normal;
                        window.Activate();
                    }
                };
                _ = InitializeAsync(desktop);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async Task InitializeAsync(IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stratum");
                Directory.CreateDirectory(dataDir);
                var databasePath = Path.Combine(dataDir, "authenticator.db3");

                var keyStore = new Services.KeyStore(dataDir);
                var key = keyStore.GetOrCreateKey();
                var password = Convert.ToBase64String(key);

                if (File.Exists(databasePath) && await Services.DatabaseMigrator.IsPlainAsync(databasePath))
                {
                    await Services.DatabaseMigrator.MigrateToEncryptedAsync(databasePath, password);
                }

                var database = new Persistence.Database(databasePath);
                await database.OpenAsync(password);

                var authenticatorRepo = new Persistence.AuthenticatorRepository(database);
                var categoryRepo = new Persistence.CategoryRepository(database);
                var authCatRepo = new Persistence.AuthenticatorCategoryRepository(database);
                var customIconRepo = new Persistence.CustomIconRepository(database);
                var iconPackRepo = new Persistence.IconPackRepository(database);
                var iconPackEntryRepo = new Persistence.IconPackEntryRepository(database);

                var customIconService = new CustomIconService(customIconRepo, authenticatorRepo);
                var authenticatorService = new AuthenticatorService(authenticatorRepo, authCatRepo,
                    customIconService, new AuthenticatorComparer());
                var categoryService = new CategoryService(categoryRepo, authCatRepo, new CategoryComparer(),
                    new AuthenticatorCategoryComparer());
                var restoreService = new RestoreService(authenticatorService, categoryService, customIconService);
                var importService = new ImportService(restoreService);
                var backupService = new BackupService(authenticatorRepo, categoryRepo, authCatRepo, customIconRepo,
                    new Services.AssetProvider(Path.Combine(AppContext.BaseDirectory, "Assets")));
                var iconPackService = new IconPackService(iconPackRepo, iconPackEntryRepo);

                var settingsService = new Services.SettingsService(Path.Combine(dataDir, "settings.json"));
                Services.Localization.Apply(Services.Localization.Resolve(settingsService.Settings.Language));
                RequestedThemeVariant = settingsService.Settings.Theme switch
                {
                    "Light" => Avalonia.Styling.ThemeVariant.Light,
                    "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                    _ => Avalonia.Styling.ThemeVariant.Default
                };

                var iconResolver = new Services.IconResolver();
                await iconResolver.InitializeAsync();

                var viewModel = new MainViewModel(authenticatorRepo, authenticatorService, backupService,
                    importService, restoreService, customIconRepo, customIconService, categoryService,
                    iconPackRepo, iconPackEntryRepo, iconPackService, iconResolver, settingsService);
                await viewModel.LoadAsync();

                if (desktop.MainWindow is MainWindow window)
                {
                    window.DataContext = viewModel;
                    viewModel.Attach(window);
                }
            }
            catch (Exception e)
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stratum");
                Directory.CreateDirectory(logDir);
                File.WriteAllText(Path.Combine(logDir, "startup-error.log"), e.ToString());
                desktop.Shutdown(1);
            }
        }
    }
}
