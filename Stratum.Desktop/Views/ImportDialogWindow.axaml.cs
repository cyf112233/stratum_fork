using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Views
{
    public partial class ImportDialogWindow : Window
    {
        public sealed record Selection(int FormatIndex, string Password);

        private Selection _selection;

        public ImportDialogWindow()
        {
            InitializeComponent();
            Icon = AppIcons.GetWindowIcon();
            var items = new List<string> { "自动检测" };

            foreach (var format in ImportFormats.All)
            {
                items.Add(format.Name);
            }

            FormatsCombo.ItemsSource = items;
            FormatsCombo.SelectedIndex = 0;
        }

        public Selection GetResult()
        {
            return _selection;
        }

        private void OnImportClicked(object sender, RoutedEventArgs e)
        {
            _selection = new Selection(FormatsCombo.SelectedIndex, PasswordBox.Text);
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
