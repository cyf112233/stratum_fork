using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ProtoBuf;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Backup.Encryption;
using Stratum.Core.Converter;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Desktop.Services;
using Stratum.Desktop.Views;

namespace Stratum.Desktop.ViewModels
{
    public class CategoryItemViewModel : ViewModelBase
    {
        private bool _isSelected;

        public CategoryItemViewModel(string categoryId, string label)
        {
            CategoryId = categoryId;
            Label = label;
        }

        public string CategoryId { get; }

        public string Label { get; }

        public bool IsRealCategory => !string.IsNullOrEmpty(CategoryId);

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public int Count { get; private set; }

        public void UpdateCount(int count)
        {
            Count = count;
            OnPropertyChanged(nameof(Count));
        }
    }

    public class MainViewModel : ViewModelBase
    {
        private readonly IAuthenticatorRepository _repository;
        private readonly IAuthenticatorService _authenticatorService;
        private readonly IBackupService _backupService;
        private readonly IImportService _importService;
        private readonly IRestoreService _restoreService;
        private readonly ICustomIconRepository _customIconRepository;
        private readonly ICustomIconService _customIconService;
        private readonly ICategoryService _categoryService;
        private readonly IIconPackRepository _iconPackRepository;
        private readonly IIconPackEntryRepository _iconPackEntryRepository;
        private readonly IIconPackService _iconPackService;
        private readonly IIconResolver _iconResolver;
        private readonly Services.SettingsService _settingsService;
        private readonly DispatcherTimer _timer;
        private readonly List<AuthenticatorItemViewModel> _allItems = new();
        private readonly Dictionary<string, List<string>> _authCategories = new();
        private CategoryItemViewModel _selectedCategory;
        private DateTime _statusUntil;
        private string _status;
        private Window _owner;

        public MainViewModel(IAuthenticatorRepository repository, IAuthenticatorService authenticatorService,
            IBackupService backupService, IImportService importService, IRestoreService restoreService,
            ICustomIconRepository customIconRepository, ICustomIconService customIconService,
            ICategoryService categoryService, IIconPackRepository iconPackRepository,
            IIconPackEntryRepository iconPackEntryRepository, IIconPackService iconPackService,
            IIconResolver iconResolver, Services.SettingsService settingsService)
        {
            _repository = repository;
            _authenticatorService = authenticatorService;
            _backupService = backupService;
            _importService = importService;
            _restoreService = restoreService;
            _customIconRepository = customIconRepository;
            _customIconService = customIconService;
            _categoryService = categoryService;
            _iconPackRepository = iconPackRepository;
            _iconPackEntryRepository = iconPackEntryRepository;
            _iconPackService = iconPackService;
            _iconResolver = iconResolver;
            _settingsService = settingsService;

            AddCommand = new RelayCommand(() => _ = AddAsync());
            CopyCommand = new RelayCommand<AuthenticatorItemViewModel>(CopyAsync);
            EditCommand = new RelayCommand<AuthenticatorItemViewModel>(EditAsync);
            ChangeIconCommand = new RelayCommand<AuthenticatorItemViewModel>(ChangeIconAsync);
            IncrementCounterCommand = new RelayCommand<AuthenticatorItemViewModel>(IncrementCounterAsync);
            DeleteCommand = new RelayCommand<AuthenticatorItemViewModel>(DeleteAsync);
            ExportCommand = new ParamRelayCommand(p => _ = ExportBackupAsync(Convert.ToInt32(p)));
            ImportCommand = new RelayCommand(() => _ = ImportBackupAsync());
            SelectCategoryCommand = new RelayCommand<CategoryItemViewModel>(item =>
            {
                SelectCategory(item);
                return Task.CompletedTask;
            });
            AddCategoryCommand = new RelayCommand(() => _ = AddCategoryAsync());
            RenameCategoryCommand = new RelayCommand<CategoryItemViewModel>(RenameCategoryAsync);
            DeleteCategoryCommand = new RelayCommand<CategoryItemViewModel>(DeleteCategoryAsync);
            ImportIconPackCommand = new RelayCommand(() => _ = ImportIconPackAsync());
            SettingsCommand = new RelayCommand(() => _ = OpenSettingsAsync());
            CardCommand = new RelayCommand<AuthenticatorItemViewModel>(OnCardClickedAsync);

            Authenticators.CollectionChanged += OnAuthenticatorsChanged;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public ObservableCollection<AuthenticatorItemViewModel> Authenticators { get; } = new();

        public ObservableCollection<CategoryItemViewModel> Categories { get; } = new();

        public RelayCommand AddCommand { get; }

        public RelayCommand<AuthenticatorItemViewModel> CopyCommand { get; }

        public RelayCommand<AuthenticatorItemViewModel> EditCommand { get; }

        public RelayCommand<AuthenticatorItemViewModel> ChangeIconCommand { get; }

        public RelayCommand<AuthenticatorItemViewModel> IncrementCounterCommand { get; }

        public RelayCommand<AuthenticatorItemViewModel> DeleteCommand { get; }

        public ParamRelayCommand ExportCommand { get; }

        public RelayCommand ImportCommand { get; }

        public RelayCommand<CategoryItemViewModel> SelectCategoryCommand { get; }

        public RelayCommand AddCategoryCommand { get; }

        public RelayCommand<CategoryItemViewModel> RenameCategoryCommand { get; }

        public RelayCommand<CategoryItemViewModel> DeleteCategoryCommand { get; }

        public RelayCommand ImportIconPackCommand { get; }

        public RelayCommand SettingsCommand { get; }

        public RelayCommand<AuthenticatorItemViewModel> CardCommand { get; }

        public bool IsEmpty => Authenticators.Count == 0;

        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(HasStatus));
                }
            }
        }

        public bool HasStatus => !string.IsNullOrEmpty(_status);

        public void Attach(Window window)
        {
            _owner = window;
        }

        public async Task LoadAsync()
        {
            var auths = await _repository.GetAllAsync();
            var categories = await _categoryService.GetAllCategoriesAsync();
            var bindings = await _categoryService.GetAllBindingsAsync();

            _authCategories.Clear();

            foreach (var binding in bindings)
            {
                if (!_authCategories.TryGetValue(binding.AuthenticatorSecret, out var list))
                {
                    _authCategories[binding.AuthenticatorSecret] = list = new List<string>();
                }

                list.Add(binding.CategoryId);
            }

            _allItems.Clear();

            foreach (var auth in auths.OrderBy(a => a.Ranking))
            {
                var item = new AuthenticatorItemViewModel(auth, _customIconRepository, _settingsService.Settings);
                _allItems.Add(item);
                await item.LoadIconAsync();
            }

            var selectedId = _selectedCategory?.CategoryId;
            Categories.Clear();

            var all = new CategoryItemViewModel(null, AppStrings.All);
            all.IsSelected = selectedId == null;
            Categories.Add(all);

            var uncategorized = new CategoryItemViewModel("", AppStrings.Uncategorized);
            uncategorized.IsSelected = selectedId == "";
            Categories.Add(uncategorized);

            foreach (var category in categories.OrderBy(c => c.Ranking))
            {
                var item = new CategoryItemViewModel(category.Id, category.Name);
                item.IsSelected = selectedId == category.Id;
                Categories.Add(item);
            }

            _selectedCategory = Categories.FirstOrDefault(c => c.IsSelected) ?? all;
            UpdateCategoryCounts();
            ApplyFilter();
            RefreshCodes();
        }

        private void UpdateCategoryCounts()
        {
            foreach (var category in Categories)
            {
                var count = category.CategoryId switch
                {
                    null => _allItems.Count,
                    "" => _allItems.Count(a =>
                        !_authCategories.TryGetValue(a.Auth.Secret, out var ids) || ids.Count == 0),
                    _ => _allItems.Count(a =>
                        _authCategories.TryGetValue(a.Auth.Secret, out var ids) && ids.Contains(category.CategoryId))
                };

                category.UpdateCount(count);
            }
        }

        private void ApplyFilter()
        {
            Authenticators.Clear();
            var id = _selectedCategory?.CategoryId;

            IEnumerable<AuthenticatorItemViewModel> items = id switch
            {
                null => _allItems,
                "" => _allItems.Where(a =>
                    !_authCategories.TryGetValue(a.Auth.Secret, out var ids) || ids.Count == 0),
                _ => _allItems.Where(a =>
                    _authCategories.TryGetValue(a.Auth.Secret, out var ids) && ids.Contains(id))
            };

            foreach (var item in items)
            {
                Authenticators.Add(item);
            }

            OnPropertyChanged(nameof(IsEmpty));
        }

        private void SelectCategory(CategoryItemViewModel item)
        {
            if (_selectedCategory != null)
            {
                _selectedCategory.IsSelected = false;
            }

            _selectedCategory = item;

            if (item != null)
            {
                item.IsSelected = true;
            }

            ApplyFilter();
        }

        private void OnAuthenticatorsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void OnTick(object sender, EventArgs e)
        {
            RefreshCodes();

            if (!string.IsNullOrEmpty(Status) && DateTime.Now > _statusUntil)
            {
                Status = null;
            }
        }

        private void RefreshCodes()
        {
            foreach (var item in Authenticators)
            {
                item.Refresh();
            }
        }

        private void ShowStatus(string message)
        {
            Status = message;
            _statusUntil = DateTime.Now.AddSeconds(5);
        }

        private async Task AddAsync()
        {
            var dialog = new AddAccountWindow(_authenticatorService, _categoryService, _iconResolver,
                _importService, _restoreService, null);
            await dialog.ShowDialog(_owner);
            await LoadAsync();
        }

        private async Task EditAsync(AuthenticatorItemViewModel item)
        {
            var dialog = new AddAccountWindow(_authenticatorService, _categoryService, _iconResolver,
                _importService, _restoreService, item.Auth);
            await dialog.ShowDialog(_owner);
            await LoadAsync();
        }

        private async Task ChangeIconAsync(AuthenticatorItemViewModel item)
        {
            var window = new ChangeIconWindow(_iconResolver, _iconPackRepository, _iconPackEntryRepository,
                _customIconService);
            await window.ShowDialog(_owner);
            var icon = window.GetResult();

            if (icon != null)
            {
                await _authenticatorService.SetIconAsync(item.Auth, icon);
                await LoadAsync();
            }
        }

        private async Task IncrementCounterAsync(AuthenticatorItemViewModel item)
        {
            await _authenticatorService.IncrementCounterAsync(item.Auth);
            await LoadAsync();
        }

        private async Task CopyAsync(AuthenticatorItemViewModel item)
        {
            var clipboard = _owner?.Clipboard;

            if (clipboard == null)
            {
                return;
            }

            await clipboard.SetTextAsync(item.Code);
            item.IsCopied = true;
            await Task.Delay(1500);
            item.IsCopied = false;
        }

        private async Task DeleteAsync(AuthenticatorItemViewModel item)
        {
            if (_settingsService.Settings.ConfirmDeletes)
            {
                var confirm = await new ConfirmWindow(AppStrings.DeleteAccountTitle, AppStrings.DeleteAccountFmt(item.Issuer))
                    .ShowDialog<bool?>(_owner);

                if (confirm != true)
                {
                    return;
                }
            }

            await _authenticatorService.DeleteWithCategoryBindingsAsync(item.Auth);
            await LoadAsync();
        }

        private async Task AddCategoryAsync()
        {
            var name = await new PasswordPromptWindow(AppStrings.NewCategoryTitle, AppStrings.CategoryNameMsg, AppStrings.CategoryNamePh, false)
                .ShowDialog<string>(_owner);

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            try
            {
                await _categoryService.AddCategoryAsync(new Category(name.Trim()));
                await LoadAsync();
            }
            catch (Exception e)
            {
                ShowStatus(AppStrings.CategoryFailed + e.Message);
            }
        }

        private async Task RenameCategoryAsync(CategoryItemViewModel item)
        {
            if (!item.IsRealCategory)
            {
                return;
            }

            var name = await new PasswordPromptWindow(AppStrings.RenameCategoryTitle, AppStrings.NewCategoryNameMsg, AppStrings.CategoryNamePh, false)
                .ShowDialog<string>(_owner);

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var category = await _categoryService.GetCategoryByIdAsync(item.CategoryId);

            if (category == null)
            {
                return;
            }

            category.Name = name.Trim();

            try
            {
                await _categoryService.UpdateManyCategoriesAsync(new[] { category });
                await LoadAsync();
            }
            catch (Exception e)
            {
                ShowStatus(AppStrings.RenameFailed + e.Message);
            }
        }

        private async Task DeleteCategoryAsync(CategoryItemViewModel item)
        {
            if (!item.IsRealCategory)
            {
                return;
            }

            var category = await _categoryService.GetCategoryByIdAsync(item.CategoryId);

            if (category == null)
            {
                return;
            }

            if (_settingsService.Settings.ConfirmDeletes)
            {
                var confirm = await new ConfirmWindow(AppStrings.DeleteCategoryTitle, AppStrings.DeleteCategoryFmt(item.Label))
                    .ShowDialog<bool?>(_owner);

                if (confirm != true)
                {
                    return;
                }
            }

            await _categoryService.DeleteWithCategoryBindingsASync(category);
            await LoadAsync();
        }

        private async Task OpenSettingsAsync()
        {
            var window = new SettingsWindow(_settingsService);
            await window.ShowDialog(_owner);
            await LoadAsync();
        }

        private async Task OnCardClickedAsync(AuthenticatorItemViewModel item)
        {
            if (_settingsService.Settings.HideCodes && item.IsCodeHidden)
            {
                item.Reveal();
            }
            else if (_settingsService.Settings.ClickToCopy)
            {
                await CopyAsync(item);
            }
        }

        private async Task ImportIconPackAsync()
        {
            try
            {
                var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = AppStrings.SelectIconPack,
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType(AppStrings.IconPackFileType) { Patterns = new[] { "*.iconpack" } },
                        FilePickerFileTypes.All
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

                IconPack pack;

                using (var memoryStream = new MemoryStream(data))
                {
                    pack = Serializer.Deserialize<IconPack>(memoryStream);
                }
                await _iconPackService.ImportPackAsync(pack);
                ShowStatus(AppStrings.IconPackImported + pack.Name);
            }
            catch (Exception e)
            {
                ShowStatus(AppStrings.IconPackFailed + e.Message);
            }
        }

        private async Task ExportBackupAsync(int format)
        {
            try
            {
                byte[] data;
                string fileName;
                string extension;

                switch (format)
                {
                    case 0:
                    {
                        var password = await new PasswordPromptWindow(AppStrings.ExportBackupTitle, AppStrings.BackupSetPasswordMsg)
                            .ShowDialog<string>(_owner);

                        if (password == null)
                        {
                            return;
                        }

                        var backup = await _backupService.CreateBackupAsync();
                        IBackupEncryption encryption = string.IsNullOrEmpty(password)
                            ? new NoBackupEncryption()
                            : new StrongBackupEncryption();
                        data = await encryption.EncryptAsync(backup, password);
                        fileName = $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.stratum";
                        extension = "stratum";
                        break;
                    }

                    case 1:
                    {
                        var html = await _backupService.CreateHtmlBackupAsync();
                        data = System.Text.Encoding.UTF8.GetBytes(html.ToString());
                        fileName = $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.html";
                        extension = "html";
                        break;
                    }

                    default:
                    {
                        var uriList = await _backupService.CreateUriListBackupAsync();
                        data = System.Text.Encoding.UTF8.GetBytes(uriList.ToString());
                        fileName = $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.txt";
                        extension = "txt";
                        break;
                    }
                }

                var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = AppStrings.SaveBackupTitle,
                    SuggestedFileName = fileName,
                    DefaultExtension = extension,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType(AppStrings.BackupFileType) { Patterns = new[] { "*." + extension } }
                    }
                });

                if (file == null)
                {
                    return;
                }

                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(data);
                ShowStatus(AppStrings.BackupExported);
            }
            catch (Exception e)
            {
                ShowStatus(AppStrings.ExportFailed + e.Message);
            }
        }

        private async Task ImportBackupAsync()
        {
            try
            {
                var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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

                var file = files[0];
                byte[] data;

                await using (var stream = await file.OpenReadAsync())
                using (var memory = new MemoryStream())
                {
                    await stream.CopyToAsync(memory);
                    data = memory.ToArray();
                }

                var name = file.Name.ToLower();

                var restored = await TryDecryptStratumBackupAsync(data);

                if (restored != null)
                {
                    await _restoreService.RestoreAndUpdateAsync(restored);
                    ShowStatus(AppStrings.RestoreComplete);
                    await LoadAsync();
                    return;
                }

                if (name.EndsWith(".stratum"))
                {
                    ShowStatus(AppStrings.DecryptFailed);
                    return;
                }

                if (name.EndsWith(".txt"))
                {
                    await ImportWithConverterAsync(new UriListBackupConverter(_iconResolver), data);
                    return;
                }

                if (name.EndsWith(".html"))
                {
                    await ImportWithConverterAsync(new HtmlBackupConverter(_iconResolver), data);
                    return;
                }

                var dialog = new ImportDialogWindow();
                await dialog.ShowDialog(_owner);
                var selection = dialog.GetResult();

                if (selection == null)
                {
                    return;
                }

                if (selection.FormatIndex == 0)
                {
                    ShowStatus(AppStrings.UnknownFormatPick);
                    return;
                }

                var format = Services.ImportFormats.All[selection.FormatIndex - 1];
                var converter = format.Create(_iconResolver, new Services.CustomIconDecoder());
                await ImportWithConverterAsync(converter, data, selection.Password);
            }
            catch (Exception e)
            {
                ShowStatus(AppStrings.ImportFailed + e.Message);
            }
        }

        private async Task<Backup> TryDecryptStratumBackupAsync(byte[] data)
        {
            var strong = new StrongBackupEncryption();
            var legacy = new LegacyBackupEncryption();
            var none = new NoBackupEncryption();

            if (none.CanBeDecrypted(data))
            {
                try
                {
                    return await none.DecryptAsync(data, null);
                }
                catch
                {
                    return null;
                }
            }

            IBackupEncryption[] encrypted = { strong, legacy };

            if (!encrypted.Any(e => e.CanBeDecrypted(data)))
            {
                return null;
            }

            while (true)
            {
                var password = await new PasswordPromptWindow(AppStrings.BackupPasswordTitle, AppStrings.BackupEncryptedMsg)
                    .ShowDialog<string>(_owner);

                if (string.IsNullOrEmpty(password))
                {
                    return null;
                }

                foreach (var encryption in encrypted)
                {
                    if (!encryption.CanBeDecrypted(data))
                    {
                        continue;
                    }

                    try
                    {
                        return await encryption.DecryptAsync(data, password);
                    }
                    catch (BackupPasswordException)
                    {
                    }
                }

                ShowStatus(AppStrings.WrongPassword);
            }
        }

        private async Task ImportWithConverterAsync(BackupConverter converter, byte[] data, string password = null)
        {
            switch (converter.PasswordPolicy)
            {
                case BackupConverter.BackupPasswordPolicy.Never:
                    break;

                case BackupConverter.BackupPasswordPolicy.Always:
                {
                    var pw = await new PasswordPromptWindow(AppStrings.EnterPasswordTitle, AppStrings.PasswordRequiredMsg)
                        .ShowDialog<string>(_owner);

                    if (pw == null || pw.Length == 0)
                    {
                        ShowStatus(AppStrings.ImportCancelledPassword);
                        return;
                    }

                    password = pw;
                    break;
                }

                case BackupConverter.BackupPasswordPolicy.Maybe:
                {
                    try
                    {
                        var (conversion, _) = await _importService.ImportAsync(converter, data, null);
                        await FinishImportAsync(conversion);
                        return;
                    }
                    catch (Exception)
                    {
                    }

                    var pw2 = await new PasswordPromptWindow(AppStrings.EnterPasswordTitle, AppStrings.MaybePasswordMsg)
                        .ShowDialog<string>(_owner);

                    if (pw2 == null)
                    {
                        ShowStatus(AppStrings.ImportCancelled);
                        return;
                    }

                    password = pw2;
                    break;
                }
            }

            var (result, _) = await _importService.ImportAsync(converter, data, password);
            await FinishImportAsync(result);
        }

        private async Task FinishImportAsync(ConversionResult conversion)
        {
            var failures = conversion.Failures?.Count() ?? 0;
            ShowStatus(failures > 0 ? AppStrings.ImportCompleteWithFailures(failures) : AppStrings.ImportComplete);
            await LoadAsync();
        }
    }
}
