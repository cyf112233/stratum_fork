using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stratum.Desktop.Services;
using Stratum.Desktop.ViewModels;

namespace Stratum.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Icon = AppIcons.GetWindowIcon();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Attach(this);
            }
        }

        private void OnCardPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);

            if (point.Properties.IsRightButtonPressed)
            {
                return;
            }

            if (DataContext is MainViewModel viewModel && e.Source is Avalonia.StyledElement element &&
                element.DataContext is AuthenticatorItemViewModel item)
            {
                viewModel.CardCommand.Execute(item);
            }
        }

        private const double ResizeEdge = 6;

        private WindowEdge? DetectEdge(Point p)
        {
            var width = Bounds.Width;
            var height = Bounds.Height;
            var left = p.X < ResizeEdge;
            var right = p.X > width - ResizeEdge;
            var top = p.Y < ResizeEdge;
            var bottom = p.Y > height - ResizeEdge;

            if (top && left) return WindowEdge.NorthWest;
            if (top && right) return WindowEdge.NorthEast;
            if (bottom && left) return WindowEdge.SouthWest;
            if (bottom && right) return WindowEdge.SouthEast;
            if (top) return WindowEdge.North;
            if (bottom) return WindowEdge.South;
            if (left) return WindowEdge.West;
            if (right) return WindowEdge.East;
            return null;
        }

        private static Cursor ResizeCursor(WindowEdge edge)
        {
            return edge switch
            {
                WindowEdge.North => new Cursor(StandardCursorType.SizeNorthSouth),
                WindowEdge.South => new Cursor(StandardCursorType.SizeNorthSouth),
                WindowEdge.East => new Cursor(StandardCursorType.SizeWestEast),
                WindowEdge.West => new Cursor(StandardCursorType.SizeWestEast),
                WindowEdge.NorthEast => new Cursor(StandardCursorType.TopRightCorner),
                WindowEdge.SouthWest => new Cursor(StandardCursorType.BottomLeftCorner),
                WindowEdge.NorthWest => new Cursor(StandardCursorType.TopLeftCorner),
                WindowEdge.SouthEast => new Cursor(StandardCursorType.BottomRightCorner),
                _ => Cursor.Default
            };
        }

        private void OnRootPointerMoved(object sender, PointerEventArgs e)
        {
            Cursor = DetectEdge(e.GetPosition(this)) is { } edge ? ResizeCursor(edge) : Cursor.Default;
        }

        private void OnRootPointerExited(object sender, PointerEventArgs e)
        {
            Cursor = Cursor.Default;
        }

        private void OnRootPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (DetectEdge(e.GetPosition(this)) is { } edge)
            {
                BeginResizeDrag(edge, e);
            }
        }

        private void OnTitlePointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.Source is Button || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            BeginMoveDrag(e);
        }

        private void OnTitleDoubleTapped(object sender, TappedEventArgs e)
        {
            if (e.Source is Button)
            {
                return;
            }

            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
