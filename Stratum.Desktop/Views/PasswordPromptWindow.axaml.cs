using Avalonia.Controls;
using Avalonia.Interactivity;
using Stratum.Desktop.Services;

namespace Stratum.Desktop.Views
{
    public partial class PasswordPromptWindow : Window
    {
        public PasswordPromptWindow(string title, string message, string placeholder = "密码", bool isPassword = true)
        {
            InitializeComponent();
            Icon = AppIcons.GetWindowIcon();
            Title = title;
            HeaderBar.TitleText = title;
            MessageText.Text = message;
            PasswordBox.PlaceholderText = placeholder;

            if (!isPassword)
            {
                PasswordBox.PasswordChar = '\0';
            }

            PasswordBox.AttachedToVisualTree += (_, _) => PasswordBox.Focus();
        }

        private void OnConfirmClicked(object sender, RoutedEventArgs e)
        {
            Close(PasswordBox.Text);
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
