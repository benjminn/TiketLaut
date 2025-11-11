using System.Windows;

namespace TiketLaut.Helpers
{
    public static class WindowSizeHelper
    {
        public static void SetResponsiveSize(Window window, double heightPercentage = 0.9, double widthPercentage = 0.9)
        {
            if (heightPercentage <= 0 || heightPercentage > 1)
                heightPercentage = 0.9;
            if (widthPercentage <= 0 || widthPercentage > 1)
                widthPercentage = 0.9;

            window.MaxHeight = SystemParameters.WorkArea.Height * heightPercentage;
            window.MaxWidth = SystemParameters.WorkArea.Width * widthPercentage;

            if (window.Height > window.MaxHeight)
                window.Height = window.MaxHeight;
            if (window.Width > window.MaxWidth)
                window.Width = window.MaxWidth;

            if (window.MinHeight == 0)
                window.MinHeight = 300;
            if (window.MinWidth == 0)
                window.MinWidth = 400;
        }

        public static void SetDetailWindowSize(Window window)
        {
            SetResponsiveSize(window, 0.9, 0.9);
        }

        public static void SetFormDialogSize(Window window)
        {
            SetResponsiveSize(window, 0.85, 0.85);
        }

        public static void SetSmallDialogSize(Window window)
        {
            SetResponsiveSize(window, 0.7, 0.7);
        }

        public static void SetLargeWindowSize(Window window)
        {
            SetResponsiveSize(window, 0.95, 0.95);
        }
    }
}
