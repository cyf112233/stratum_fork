using Avalonia.Controls;
using Avalonia.Interactivity;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Views
{
    public partial class ConfirmWindow : Window
    {
        public ConfirmWindow(string title, string message)
        {
            InitializeComponent();
            Icon = AppIcons.GetWindowIcon();
            Title = title;
            HeaderBar.TitleText = title;
            MessageText.Text = message;
        }

        private void OnConfirmClicked(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
