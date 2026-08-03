using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Stratum.Desktop.Controls
{
    public partial class TitleBar : UserControl
    {
        public static readonly StyledProperty<string> TitleTextProperty =
            AvaloniaProperty.Register<TitleBar, string>(nameof(TitleText), "");

        public TitleBar()
        {
            InitializeComponent();
        }

        public string TitleText
        {
            get => GetValue(TitleTextProperty);
            set => SetValue(TitleTextProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TitleTextProperty)
            {
                TitleTextBlock.Text = TitleText;
            }
        }

        private void OnTitlePointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.Source is Button || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.BeginMoveDrag(e);
            }
        }

        private void OnTitleDoubleTapped(object sender, TappedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void OnMaximizeClicked(object sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            (TopLevel.GetTopLevel(this) as Window)?.Close();
        }
    }
}
