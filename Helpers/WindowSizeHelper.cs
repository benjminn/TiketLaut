using System.Windows;

namespace TiketLaut.Helpers
{
    /// <summary>
    /// Helper class untuk mengatur ukuran window agar responsif terhadap ukuran layar
    /// </summary>
    public static class WindowSizeHelper
    {
        /// <summary>
        /// Mengatur MaxHeight dan MaxWidth window berdasarkan ukuran layar
        /// </summary>
        /// <param name="window">Window yang akan diatur</param>
        /// <param name="heightPercentage">Persentase tinggi layar (0.0 - 1.0), default 0.9 (90%)</param>
        /// <param name="widthPercentage">Persentase lebar layar (0.0 - 1.0), default 0.9 (90%)</param>
        public static void SetResponsiveSize(Window window, double heightPercentage = 0.9, double widthPercentage = 0.9)
        {
            // Pastikan persentase valid
            if (heightPercentage <= 0 || heightPercentage > 1)
                heightPercentage = 0.9;
            if (widthPercentage <= 0 || widthPercentage > 1)
                widthPercentage = 0.9;

            // Set MaxHeight dan MaxWidth berdasarkan ukuran layar kerja (working area)
            // WorkArea adalah area layar dikurangi taskbar
            window.MaxHeight = SystemParameters.WorkArea.Height * heightPercentage;
            window.MaxWidth = SystemParameters.WorkArea.Width * widthPercentage;

            // Jika window saat ini lebih besar dari max, resize ke max
            if (window.Height > window.MaxHeight)
                window.Height = window.MaxHeight;
            if (window.Width > window.MaxWidth)
                window.Width = window.MaxWidth;

            // Pastikan window tidak terlalu kecil jika MinHeight/MinWidth belum diset
            if (window.MinHeight == 0)
                window.MinHeight = 300;
            if (window.MinWidth == 0)
                window.MinWidth = 400;
        }

        /// <summary>
        /// Mengatur ukuran window untuk detail window (lebih besar)
        /// </summary>
        public static void SetDetailWindowSize(Window window)
        {
            SetResponsiveSize(window, 0.9, 0.9);
        }

        /// <summary>
        /// Mengatur ukuran window untuk form/dialog (sedang)
        /// </summary>
        public static void SetFormDialogSize(Window window)
        {
            SetResponsiveSize(window, 0.85, 0.85);
        }

        /// <summary>
        /// Mengatur ukuran window untuk dialog kecil
        /// </summary>
        public static void SetSmallDialogSize(Window window)
        {
            SetResponsiveSize(window, 0.7, 0.7);
        }

        /// <summary>
        /// Mengatur ukuran window untuk window sangat besar (seperti jadwal detail dengan banyak data)
        /// </summary>
        public static void SetLargeWindowSize(Window window)
        {
            SetResponsiveSize(window, 0.95, 0.95);
        }
    }
}
