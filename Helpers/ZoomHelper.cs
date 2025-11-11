using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TiketLaut.Helpers
{
    /// <summary>
    /// Helper class untuk menambahkan fungsi zoom (Ctrl+, Ctrl-, Ctrl+0) ke Window
    /// </summary>
    public static class ZoomHelper
    {
        // Attached property untuk track zoom level
        private static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.RegisterAttached(
                "ZoomLevel",
                typeof(double),
                typeof(ZoomHelper),
                new PropertyMetadata(1.0));

        private const double MinZoom = 0.5;  // Minimum 50%
        private const double MaxZoom = 2.0;  // Maximum 200%
        private const double ZoomStep = 0.1; // Zoom increment/decrement

        /// <summary>
        /// Enable zoom functionality pada Window
        /// Panggil method ini di constructor Window setelah InitializeComponent()
        /// </summary>
        /// <param name="window">Window yang akan diberi fungsi zoom</param>
        public static void EnableZoom(Window window)
        {
            if (window == null) return;

            // Set initial zoom level
            window.SetValue(ZoomLevelProperty, 1.0);

            // Add keyboard event handler
            window.PreviewKeyDown += (sender, e) => HandleKeyDown(window, e);
        }

        private static void HandleKeyDown(Window window, KeyEventArgs e)
        {
            // Check if Ctrl is pressed
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add) // Ctrl + (+)
                {
                    ZoomIn(window);
                    e.Handled = true;
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract) // Ctrl + (-)
                {
                    ZoomOut(window);
                    e.Handled = true;
                }
                else if (e.Key == Key.D0 || e.Key == Key.NumPad0) // Ctrl + 0 (reset)
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
            // Apply ScaleTransform to the window content
            var content = window.Content as FrameworkElement;
            if (content != null)
            {
                var scaleTransform = new ScaleTransform(zoomLevel, zoomLevel);
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(scaleTransform);
                
                content.LayoutTransform = transformGroup;
                
                // Log zoom level for debugging
                var zoomPercentage = (int)(zoomLevel * 100);
                System.Diagnostics.Debug.WriteLine($"[{window.GetType().Name}] Zoom: {zoomPercentage}%");
            }
        }
    }
}
