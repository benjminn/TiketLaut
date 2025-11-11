using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TiketLaut.Helpers
{
    public static class ZoomHelper
    {
        private static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.RegisterAttached(
                "ZoomLevel",
                typeof(double),
                typeof(ZoomHelper),
                new PropertyMetadata(1.0));

        private const double MinZoom = 0.5;
        private const double MaxZoom = 2.0;
        private const double ZoomStep = 0.1;

        public static void EnableZoom(Window window)
        {
            if (window == null) return;

            window.SetValue(ZoomLevelProperty, 1.0);

            window.PreviewKeyDown += (sender, e) => HandleKeyDown(window, e);
        }

        private static void HandleKeyDown(Window window, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)
                {
                    ZoomIn(window);
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                {
                    ZoomOut(window);
                    e.Handled = true;
                }
                else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
                {
                    ResetZoom(window);
                    e.Handled = true;
                }
            }
        }

        private static void ZoomIn(Window window)
        {
            double currentZoom = (double)window.GetValue(ZoomLevelProperty);
            if (currentZoom < MaxZoom)
            {
                double newZoom = Math.Min(currentZoom + ZoomStep, MaxZoom);
                window.SetValue(ZoomLevelProperty, newZoom);
                ApplyZoom(window, newZoom);
            }
        }

        private static void ZoomOut(Window window)
        {
            double currentZoom = (double)window.GetValue(ZoomLevelProperty);
            if (currentZoom > MinZoom)
            {
                double newZoom = Math.Max(currentZoom - ZoomStep, MinZoom);
                window.SetValue(ZoomLevelProperty, newZoom);
                ApplyZoom(window, newZoom);
            }
        }

        private static void ResetZoom(Window window)
        {
            window.SetValue(ZoomLevelProperty, 1.0);
            ApplyZoom(window, 1.0);
        }

        private static void ApplyZoom(Window window, double zoomLevel)
        {
            var content = window.Content as FrameworkElement;
            if (content != null)
            {
                var scaleTransform = new ScaleTransform(zoomLevel, zoomLevel);
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(scaleTransform);
                
                content.LayoutTransform = transformGroup;
                
                var zoomPercentage = (int)(zoomLevel * 100);
                System.Diagnostics.Debug.WriteLine($"[{window.GetType().Name}] Zoom: {zoomPercentage}%");
            }
        }
    }
}
