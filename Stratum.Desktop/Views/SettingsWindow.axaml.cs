using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;

            LanguageCombo.SelectedIndex = _settingsService.Settings.Language switch
            {
                "zh" => 1,
                "en" => 2,
                _ => 0
            };

            ThemeCombo.SelectedIndex = _settingsService.Settings.Theme switch
            {
                "Light" => 1,
                "Dark" => 2,
                _ => 0
            };

            ClickToCopySwitch.IsChecked = _settingsService.Settings.ClickToCopy;
            HideCodesSwitch.IsChecked = _settingsService.Settings.HideCodes;
            ConfirmDeleteSwitch.IsChecked = _settingsService.Settings.ConfirmDeletes;

            LanguageCombo.SelectionChanged += (_, _) => ApplyLanguage();
            ThemeCombo.SelectionChanged += (_, _) => ApplyTheme();
            ClickToCopySwitch.IsCheckedChanged += (_, _) =>
            {
                _settingsService.Settings.ClickToCopy = ClickToCopySwitch.IsChecked == true;
                _settingsService.Save();
            };
            HideCodesSwitch.IsCheckedChanged += (_, _) =>
            {
                _settingsService.Settings.HideCodes = HideCodesSwitch.IsChecked == true;
                _settingsService.Save();
            };
            ConfirmDeleteSwitch.IsCheckedChanged += (_, _) =>
            {
                _settingsService.Settings.ConfirmDeletes = ConfirmDeleteSwitch.IsChecked == true;
                _settingsService.Save();
            };
        }

        private void ApplyLanguage()
        {
            var language = LanguageCombo.SelectedIndex switch
            {
                1 => "zh",
                2 => "en",
                _ => "Auto"
            };

            _settingsService.Settings.Language = language;
            _settingsService.Save();
            Localization.Apply(Localization.Resolve(language));
        }

        private void ApplyTheme()
        {
            var variant = ThemeCombo.SelectedIndex switch
            {
                1 => ThemeVariant.Light,
                2 => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            Application.Current.RequestedThemeVariant = variant;
            _settingsService.Settings.Theme = ThemeCombo.SelectedIndex switch
            {
                1 => "Light",
                2 => "Dark",
                _ => "Auto"
            };
            _settingsService.Save();
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
