using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Backup;
using Stratum.Core.Converter;
using Stratum.Core.Entity;
using Stratum.Core.Generator;
using Stratum.Core.Persistence;
using Stratum.Core.Service;
using Stratum.Core.Util;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.ViewModels
{
    public sealed record TypeOption(AuthenticatorType Value, string Label)
    {
        public override string ToString() => Label;
    }

    public sealed record CategoryOption(string CategoryId, string Label)
    {
        public override string ToString() => Label;
    }

    public class AddAccountViewModel : ViewModelBase
    {
        private static readonly (string English, string Chinese)[] ErrorTranslations =
        {
            ("Issuer cannot be null or empty", "请填写服务商名称"),
            ("URI is not a valid otpauth", "无效的 otpauth 链接"),
            ("Secret parameter is required", "缺少密钥参数"),
            ("Unknown URI scheme", "无法识别的链接类型"),
            ("Digits parameter cannot be parsed", "位数参数无效"),
            ("Period parameter cannot be parsed", "周期参数无效"),
            ("Counter parameter cannot be parsed", "计数器参数无效"),
            ("Category name", "分类名称无效"),
            ("Cannot be null or empty", "不能为空")
        };

        private readonly IAuthenticatorService _authenticatorService;
        private readonly ICategoryService _categoryService;
        private readonly IIconResolver _iconResolver;
        private readonly Authenticator _existing;
        private readonly bool _isEdit;

        private TypeOption _selectedTypeOption;
        private CategoryOption _selectedCategoryOption;
        private string _issuer;
        private string _username;
        private string _secret;
        private string _pin;
        private int _algorithmIndex;
        private int _digits = 6;
        private int _period = 30;
        private long _counter;
        private string _uri;
        private string _error;
        private Authenticator _scannedAuth;
        private string _scanStatus;
        private string _scanPin;

        private readonly BackupImporter _backupImporter;
        private byte[] _importData;
        private string _importName;
        private BackupConverter _pendingConverter;
        private string _importStatus;
        private string _importPassword;
        private bool _importNeedsFormat;
        private bool _importNeedsPassword;
        private int _selectedImportFormatIndex;

        public List<TypeOption> TypeOptions { get; } = new()
        {
            new TypeOption(AuthenticatorType.Totp, AppStrings.TypeTotp),
            new TypeOption(AuthenticatorType.Hotp, AppStrings.TypeHotp),
            new TypeOption(AuthenticatorType.SteamOtp, AppStrings.TypeSteam),
            new TypeOption(AuthenticatorType.MobileOtp, AppStrings.TypeMotp),
            new TypeOption(AuthenticatorType.YandexOtp, AppStrings.TypeYandex)
        };

        public List<string> AlgorithmLabels { get; } = new() { "SHA1", "SHA256", "SHA512" };

        public List<int> DigitsOptions { get; private set; } = new() { 6, 7, 8 };

        public List<int> PeriodOptions { get; } = new() { 30, 60, 90, 120, 300 };

        public List<CategoryOption> CategoryOptions { get; private set; } = new();

        public event Action RequestClose;

        public AddAccountViewModel(IAuthenticatorService authenticatorService, ICategoryService categoryService,
            IIconResolver iconResolver, IImportService importService, IRestoreService restoreService,
            Authenticator existing)
        {
            _authenticatorService = authenticatorService;
            _categoryService = categoryService;
            _iconResolver = iconResolver;
            _existing = existing;
            _isEdit = existing != null;

            _backupImporter = new BackupImporter(importService, restoreService, iconResolver);

            if (_isEdit)
            {
                _selectedTypeOption = TypeOptions.FirstOrDefault(o => o.Value == existing.Type) ?? TypeOptions[0];
                _issuer = existing.Issuer;
                _username = existing.Username;
                _secret = existing.Secret;
                _pin = existing.Pin;
                _algorithmIndex = (int)existing.Algorithm;
                _digits = existing.Digits;
                _period = existing.Period;
                _counter = existing.Counter;
            }
            else
            {
                _selectedTypeOption = TypeOptions[0];
            }

            RefreshDerivedOptions();

            ParseUriCommand = new RelayCommand(TryParseUri);
            AddCommand = new AsyncRelayCommand(async () =>
            {
                if (await SaveAsync())
                {
                    RequestClose?.Invoke();
                }
            });
            ScanAddCommand = new AsyncRelayCommand(async () =>
            {
                if (await ScanAddAsync())
                {
                    RequestClose?.Invoke();
                }
            });
            ImportCommand = new AsyncRelayCommand(async () =>
            {
                if (await ConfirmImportAsync())
                {
                    RequestClose?.Invoke();
                }
            });
        }

        public RelayCommand ParseUriCommand { get; }

        public AsyncRelayCommand AddCommand { get; }

        public AsyncRelayCommand ScanAddCommand { get; }

        public AsyncRelayCommand ImportCommand { get; }

        public bool IsEdit => _isEdit;

        public TypeOption SelectedTypeOption
        {
            get => _selectedTypeOption;
            set
            {
                if (SetProperty(ref _selectedTypeOption, value))
                {
                    RefreshDerivedOptions();
                }
            }
        }

        public CategoryOption SelectedCategoryOption
        {
            get => _selectedCategoryOption;
            set => SetProperty(ref _selectedCategoryOption, value);
        }

        public string Issuer
        {
            get => _issuer;
            set => SetProperty(ref _issuer, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Secret
        {
            get => _secret;
            set => SetProperty(ref _secret, value);
        }

        public string Pin
        {
            get => _pin;
            set => SetProperty(ref _pin, value);
        }

        public int AlgorithmIndex
        {
            get => _algorithmIndex;
            set => SetProperty(ref _algorithmIndex, value);
        }

        public int Digits
        {
            get => _digits;
            set => SetProperty(ref _digits, value);
        }

        public int Period
        {
            get => _period;
            set => SetProperty(ref _period, value);
        }

        public long Counter
        {
            get => _counter;
            set => SetProperty(ref _counter, value);
        }

        public string Uri
        {
            get => _uri;
            set => SetProperty(ref _uri, value);
        }

        public string Error
        {
            get => _error;
            set
            {
                if (SetProperty(ref _error, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrEmpty(_error);

        public string ScanStatus
        {
            get => _scanStatus;
            set => SetProperty(ref _scanStatus, value);
        }

        public string ScanPin
        {
            get => _scanPin;
            set => SetProperty(ref _scanPin, value);
        }

        public List<string> ImportFormatOptions { get; } = BuildImportFormats();

        public bool HasImportFile => _importData != null;

        public string ImportStatus
        {
            get => _importStatus;
            set => SetProperty(ref _importStatus, value);
        }

        public string ImportPassword
        {
            get => _importPassword;
            set => SetProperty(ref _importPassword, value);
        }

        public bool ImportNeedsFormat
        {
            get => _importNeedsFormat;
            set => SetProperty(ref _importNeedsFormat, value);
        }

        public bool ImportNeedsPassword
        {
            get => _importNeedsPassword;
            set => SetProperty(ref _importNeedsPassword, value);
        }

        public int SelectedImportFormatIndex
        {
            get => _selectedImportFormatIndex;
            set => SetProperty(ref _selectedImportFormatIndex, value);
        }

        public bool HasScannedAuth => _scannedAuth != null;

        public bool ScanNeedsPin => _scannedAuth?.Type.HasPin() == true;

        public string ScanSummary => _scannedAuth == null
            ? null
            : string.IsNullOrEmpty(_scannedAuth.Username)
                ? _scannedAuth.Issuer
                : $"{_scannedAuth.Issuer} · {_scannedAuth.Username}";

        public bool HasVariableAlgorithm => SelectedTypeOption.Value.HasVariableAlgorithm();

        public bool HasVariablePeriod => SelectedTypeOption.Value.HasVariablePeriod();

        public bool IsTimeBased => SelectedTypeOption.Value.GetGenerationMethod() == GenerationMethod.Time;

        public bool HasPin => SelectedTypeOption.Value.HasPin();

        public bool IsHotp => SelectedTypeOption.Value == AuthenticatorType.Hotp;

        private AuthenticatorType SelectedType => SelectedTypeOption.Value;

        private HashAlgorithm SelectedAlgorithm => (HashAlgorithm)AlgorithmIndex;

        public async Task InitializeAsync()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            var options = new List<CategoryOption> { new CategoryOption(null, AppStrings.NoCategory) };

            foreach (var category in categories.OrderBy(c => c.Ranking))
            {
                options.Add(new CategoryOption(category.Id, category.Name));
            }

            CategoryOptions = options;
            OnPropertyChanged(nameof(CategoryOptions));

            if (_isEdit)
            {
                var bindings = await _categoryService.GetBindingsForAuthenticatorAsync(_existing);
                var categoryId = bindings.FirstOrDefault()?.CategoryId;
                SelectedCategoryOption = CategoryOptions.FirstOrDefault(o => o.CategoryId == categoryId)
                    ?? CategoryOptions[0];
            }
            else
            {
                SelectedCategoryOption = CategoryOptions[0];
            }
        }

        private void RefreshDerivedOptions()
        {
            DigitsOptions = Enumerable
                .Range(SelectedType.GetMinDigits(), SelectedType.GetMaxDigits() - SelectedType.GetMinDigits() + 1)
                .ToList();
            OnPropertyChanged(nameof(DigitsOptions));
            Digits = SelectedType.GetDefaultDigits();
            Period = SelectedType.GetDefaultPeriod();
            OnPropertyChanged(nameof(HasVariableAlgorithm));
            OnPropertyChanged(nameof(HasVariablePeriod));
            OnPropertyChanged(nameof(IsTimeBased));
            OnPropertyChanged(nameof(HasPin));
            OnPropertyChanged(nameof(IsHotp));
        }

        private void TryParseUri()
        {
            Error = null;

            try
            {
                var result = Stratum.Core.UriParser.ParseStandardUri(Uri.Trim(), _iconResolver);
                var auth = result.Authenticator;
                SelectedTypeOption = TypeOptions.First(o => o.Value == auth.Type);
                Issuer = auth.Issuer;
                Username = auth.Username;
                Secret = auth.Secret;
                Pin = auth.Pin;
                AlgorithmIndex = (int)auth.Algorithm;
                Digits = auth.Digits;
                Period = auth.Period;
                Counter = auth.Counter;
            }
            catch (Exception e)
            {
                Error = TranslateError(e.Message);
            }
        }

        private async Task<bool> SaveAsync()
        {
            Error = null;

            try
            {
                if (HasPin && string.IsNullOrWhiteSpace(Pin))
                {
                    Error = AppStrings.PinRequired;
                    return false;
                }

                var auth = new Authenticator
                {
                    Type = SelectedType,
                    Issuer = Issuer?.Trim(),
                    Username = string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                    Secret = SecretUtil.Normalise(Secret, SelectedType),
                    Pin = string.IsNullOrWhiteSpace(Pin) ? null : Pin.Trim(),
                    Algorithm = SelectedAlgorithm,
                    Digits = Digits,
                    Period = Period,
                    Counter = Counter,
                    Icon = _iconResolver.FindServiceKeyByName(Issuer?.Trim())
                };

                auth.Validate();

                if (_isEdit)
                {
                    var changedSecret = auth.Secret != _existing.Secret;

                    if (changedSecret)
                    {
                        await _authenticatorService.ChangeSecretAsync(_existing, auth.Secret);
                        _existing.Secret = auth.Secret;
                    }

                    _existing.Type = auth.Type;
                    _existing.Issuer = auth.Issuer;
                    _existing.Username = auth.Username;
                    _existing.Pin = auth.Pin;
                    _existing.Algorithm = auth.Algorithm;
                    _existing.Digits = auth.Digits;
                    _existing.Period = auth.Period;
                    _existing.Counter = auth.Counter;
                    _existing.Icon = auth.Icon;
                    await _authenticatorService.UpdateAsync(_existing);
                    await UpdateCategoryBindingAsync();
                }
                else
                {
                    await _authenticatorService.AddAsync(auth);

                    if (SelectedCategoryOption?.CategoryId != null)
                    {
                        var category = await _categoryService.GetCategoryByIdAsync(SelectedCategoryOption.CategoryId);

                        if (category != null)
                        {
                            await _categoryService.AddBindingAsync(auth, category);
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Error = TranslateError(e.Message);
                return false;
            }
        }

        private async Task UpdateCategoryBindingAsync()
        {
            if (!_isEdit)
            {
                return;
            }

            var oldBindings = await _categoryService.GetBindingsForAuthenticatorAsync(_existing);
            var oldId = oldBindings.FirstOrDefault()?.CategoryId;
            var newId = SelectedCategoryOption?.CategoryId;

            if (oldId == newId)
            {
                return;
            }

            if (oldId != null)
            {
                var oldCategory = await _categoryService.GetCategoryByIdAsync(oldId);

                if (oldCategory != null)
                {
                    await _categoryService.RemoveBindingAsync(_existing, oldCategory);
                }
            }

            if (newId != null)
            {
                var newCategory = await _categoryService.GetCategoryByIdAsync(newId);

                if (newCategory != null)
                {
                    await _categoryService.AddBindingAsync(_existing, newCategory);
                }
            }
        }

        public void SetScannedUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                _scannedAuth = null;
                ScanStatus = AppStrings.ScanNone;
                OnPropertyChanged(nameof(HasScannedAuth));
                OnPropertyChanged(nameof(ScanNeedsPin));
                OnPropertyChanged(nameof(ScanSummary));
                return;
            }

            try
            {
                var result = Stratum.Core.UriParser.ParseStandardUri(uri, _iconResolver);
                _scannedAuth = result.Authenticator;
                ScanStatus = AppStrings.ScanOk;
                ScanPin = null;
            }
            catch (Exception e)
            {
                _scannedAuth = null;
                ScanStatus = AppStrings.ScanFailed + TranslateError(e.Message);
            }

            OnPropertyChanged(nameof(HasScannedAuth));
            OnPropertyChanged(nameof(ScanNeedsPin));
            OnPropertyChanged(nameof(ScanSummary));
        }

        private async Task<bool> ScanAddAsync()
        {
            Error = null;

            try
            {
                if (_scannedAuth == null)
                {
                    return false;
                }

                if (ScanNeedsPin && string.IsNullOrWhiteSpace(ScanPin))
                {
                    Error = AppStrings.PinRequired;
                    return false;
                }

                var auth = new Authenticator
                {
                    Type = _scannedAuth.Type,
                    Issuer = _scannedAuth.Issuer,
                    Username = _scannedAuth.Username,
                    Secret = _scannedAuth.Secret,
                    Pin = string.IsNullOrWhiteSpace(ScanPin) ? _scannedAuth.Pin : ScanPin.Trim(),
                    Algorithm = _scannedAuth.Algorithm,
                    Digits = _scannedAuth.Digits,
                    Period = _scannedAuth.Period,
                    Counter = _scannedAuth.Counter,
                    Icon = _iconResolver.FindServiceKeyByName(_scannedAuth.Issuer)
                };

                auth.Validate();
                await _authenticatorService.AddAsync(auth);

                if (SelectedCategoryOption?.CategoryId != null)
                {
                    var category = await _categoryService.GetCategoryByIdAsync(SelectedCategoryOption.CategoryId);

                    if (category != null)
                    {
                        await _categoryService.AddBindingAsync(auth, category);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Error = TranslateError(e.Message);
                return false;
            }
        }

        private static List<string> BuildImportFormats()
        {
            var list = new List<string> { AppStrings.AutoDetect };

            foreach (var format in Services.ImportFormats.All)
            {
                list.Add(format.Name);
            }

            return list;
        }

        public void SetImportData(byte[] data, string fileName)
        {
            _importData = data;
            _importName = fileName;
            _pendingConverter = null;
            ImportStatus = null;
            ImportNeedsFormat = false;
            ImportNeedsPassword = false;
            ImportPassword = null;
            OnPropertyChanged(nameof(HasImportFile));
            _ = TryAutoImportAsync();
        }

        private async Task TryAutoImportAsync()
        {
            if (_importData == null)
            {
                return;
            }

            try
            {
                var name = _importName.ToLower();
                var plain = await _backupImporter.TryReadPlainAsync(_importData);

                if (plain != null)
                {
                    await _backupImporter.RestoreAsync(plain);
                    ImportStatus = AppStrings.RestoreComplete;
                    RequestClose?.Invoke();
                    return;
                }

                if (_backupImporter.IsStratumEncrypted(_importData))
                {
                    ImportNeedsPassword = true;
                    ImportStatus = AppStrings.ImportNeedsPassword;
                    return;
                }

                if (name.EndsWith(".txt"))
                {
                    await _backupImporter.ImportWithConverterAsync(new UriListBackupConverter(_iconResolver),
                        _importData, null);
                    ImportStatus = AppStrings.ImportComplete;
                    RequestClose?.Invoke();
                    return;
                }

                if (name.EndsWith(".html"))
                {
                    await _backupImporter.ImportWithConverterAsync(new HtmlBackupConverter(_iconResolver),
                        _importData, null);
                    ImportStatus = AppStrings.ImportComplete;
                    RequestClose?.Invoke();
                    return;
                }

                ImportNeedsFormat = true;
                SelectedImportFormatIndex = 0;
                ImportStatus = AppStrings.UnknownFormatPick;
            }
            catch (System.Exception e)
            {
                ImportStatus = AppStrings.ImportFailed + e.Message;
            }
        }

        private async Task<bool> ConfirmImportAsync()
        {
            if (_importData == null)
            {
                return false;
            }

            try
            {
                if (ImportNeedsPassword && _pendingConverter == null)
                {
                    var backup = await _backupImporter.TryDecryptStratumAsync(_importData, ImportPassword);

                    if (backup == null)
                    {
                        ImportStatus = AppStrings.WrongPassword;
                        return false;
                    }

                    await _backupImporter.RestoreAsync(backup);
                    ImportStatus = AppStrings.RestoreComplete;
                    return true;
                }

                if (_pendingConverter != null)
                {
                    var result = await _backupImporter.ImportWithConverterAsync(_pendingConverter, _importData,
                        ImportPassword);
                    return FinishConverterImport(result);
                }

                if (ImportNeedsFormat)
                {
                    if (SelectedImportFormatIndex <= 0)
                    {
                        ImportStatus = AppStrings.UnknownFormatPick;
                        return false;
                    }

                    var converter = _backupImporter.CreateConverter(SelectedImportFormatIndex);

                    if (converter == null)
                    {
                        return false;
                    }

                    switch (converter.PasswordPolicy)
                    {
                        case BackupConverter.BackupPasswordPolicy.Never:
                        {
                            var neverResult = await _backupImporter.ImportWithConverterAsync(converter, _importData,
                                null);
                            return FinishConverterImport(neverResult);
                        }

                        case BackupConverter.BackupPasswordPolicy.Always:
                        {
                            if (string.IsNullOrEmpty(ImportPassword))
                            {
                                _pendingConverter = converter;
                                ImportNeedsPassword = true;
                                ImportStatus = AppStrings.PasswordRequiredMsg;
                                return false;
                            }

                            var alwaysResult = await _backupImporter.ImportWithConverterAsync(converter, _importData,
                                ImportPassword);
                            return FinishConverterImport(alwaysResult);
                        }

                        default:
                        {
                            try
                            {
                                var maybeResult = await _backupImporter.ImportWithConverterAsync(converter,
                                    _importData, null);
                                return FinishConverterImport(maybeResult);
                            }
                            catch
                            {
                                _pendingConverter = converter;
                                ImportNeedsPassword = true;
                                ImportStatus = AppStrings.MaybePasswordMsg;
                                return false;
                            }
                        }
                    }
                }

                return false;
            }
            catch (System.Exception e)
            {
                ImportStatus = AppStrings.ImportFailed + e.Message;
                return false;
            }
        }

        private bool FinishConverterImport(ConversionResult conversion)
        {
            var failures = conversion.Failures?.Count() ?? 0;
            ImportStatus = failures > 0
                ? AppStrings.ImportCompleteWithFailures(failures)
                : AppStrings.ImportComplete;
            return true;
        }

        private static string TranslateError(string message)
        {
            if (AppStrings.IsEnglish)
            {
                return message;
            }

            foreach (var (english, chinese) in ErrorTranslations)
            {
                if (message.Contains(english, StringComparison.OrdinalIgnoreCase))
                {
                    return chinese;
                }
            }

            return message;
        }
    }
}
