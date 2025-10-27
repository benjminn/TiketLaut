using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TiketLaut.Views
{
    /// <summary>
    /// Interaction logic for ScheduleWindow.xaml
    /// </summary>
    public partial class ScheduleWindow : Window
    {
        public ScheduleWindow()
        {
            InitializeComponent();
            LoadScheduleData();
<<<<<<< Updated upstream
=======

            // Set user info di navbar
            navbarPostLogin.SetUserInfo("Admin User");
>>>>>>> Stashed changes
        }

        private void LoadScheduleData()
        {
            // Method ini bisa diperluas untuk memuat data jadwal dari database
            // Saat ini menggunakan data statis yang sudah ada di XAML
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Kembali ke HomePage
            HomePage homePage = new HomePage();
            homePage.Show();
            this.Close();
        }

        private void CmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
<<<<<<< Updated upstream
            // Filter jadwal berdasarkan dropdown yang dipilih
            FilterSchedules();
        }

        private void BtnFilterSearch_Click(object sender, RoutedEventArgs e)
        {
            // Apply new search filters
            FilterSchedules();

            MessageBox.Show("Filter pencarian telah diaplikasikan!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilterSchedules()
        {
            // Implementasi filtering berdasarkan dropdown yang dipilih
            var selectedFrom = cmbFilterFrom?.SelectedItem as ComboBoxItem;
            var selectedTo = cmbFilterTo?.SelectedItem as ComboBoxItem;
            var selectedDate = cmbFilterDate?.SelectedItem as ComboBoxItem;
            var selectedTime = cmbFilterTime?.SelectedItem as ComboBoxItem;
            var selectedVehicle = cmbFilterVehicle?.SelectedItem as ComboBoxItem;
            var selectedPassenger = cmbFilterPassenger?.SelectedItem as ComboBoxItem;

            // Log the selected values for debugging (bisa dihapus nanti)
            string filterInfo = $"Filter Applied:\n" +
                               $"From: {selectedFrom?.Content}\n" +
                               $"To: {selectedTo?.Content}\n" +
                               $"Date: {selectedDate?.Content}\n" +
                               $"Time: {selectedTime?.Content}\n" +
                               $"Vehicle: {selectedVehicle?.Content}\n" +
                               $"Passengers: {selectedPassenger?.Content}";

            // Di implementasi nyata, ini akan memfilter data dari database
            // Untuk demo, kita hanya menampilkan info
            System.Diagnostics.Debug.WriteLine(filterInfo);
=======
            MessageBox.Show("Fungsi pencarian akan diimplementasikan dengan filter yang dipilih",
                           "Info", MessageBoxButton.OK, MessageBoxImage.Information);
>>>>>>> Stashed changes
        }

        private void BtnPilihTiket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            string scheduleType = "";

            if (button.Name == "btnPilihTiket1" || button.Name == "btnPilihTiket3")
                scheduleType = "Regular";
            else if (button.Name == "btnPilihTiket2")
                scheduleType = "Express";

            MessageBoxResult result = MessageBox.Show(
                $"Anda memilih tiket {scheduleType}.\n\nLanjutkan ke pembayaran?",
                "Konfirmasi Pilihan Tiket",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
<<<<<<< Updated upstream
                // Di sini bisa diarahkan ke halaman pembayaran
                MessageBox.Show("Mengarahkan ke halaman pembayaran...", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Contoh: Buka window pembayaran
                // PaymentWindow paymentWindow = new PaymentWindow();
                // paymentWindow.Show();
                // this.Close();
=======
                // Check if the selected ticket is "Reguler"
                if (schedule.FerryType.Equals("Reguler", StringComparison.OrdinalIgnoreCase))
                {
                    // Navigate to BookingDetailWindow for regular tickets
                    var bookingDetailWindow = new BookingDetailWindow();

                    // Pass the selected schedule data to the booking detail window
                    bookingDetailWindow.SetScheduleData(schedule);

                    // Maintain window properties
                    bookingDetailWindow.Left = this.Left;
                    bookingDetailWindow.Top = this.Top;
                    bookingDetailWindow.Width = this.Width;
                    bookingDetailWindow.Height = this.Height;
                    bookingDetailWindow.WindowState = this.WindowState;

                    // Show the booking detail window and close current window
                    bookingDetailWindow.Show();
                    this.Close();
                }
                else
                {
                    // For non-regular tickets (Express, etc.), show a message or handle differently
                    MessageBox.Show($"Anda memilih tiket {schedule.FerryType}\n" +
                                   $"Keberangkatan: {schedule.DepartureTime} - {schedule.ArrivalTime}\n" +
                                   $"Harga: {schedule.Price}\n\n" +
                                   $"Fitur pemesanan untuk {schedule.FerryType} akan segera tersedia.",
                                   "Pilih Tiket", MessageBoxButton.OK, MessageBoxImage.Information);
                }
>>>>>>> Stashed changes
            }
        }



        private void BtnToggleFacilities_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            Border? facilitiesPanel = null;

            // Tentukan panel mana yang akan di-toggle berdasarkan button yang diklik
            if (button.Name == "btnToggleFacilities1")
            {
                facilitiesPanel = facilitiesPanel1;
            }
            else if (button.Name == "btnToggleFacilities2")
            {
                facilitiesPanel = facilitiesPanel2;
            }
            else if (button.Name == "btnToggleFacilities3")
            {
                facilitiesPanel = facilitiesPanel3;
            }

            if (facilitiesPanel != null)
            {
                // Toggle visibility
                if (facilitiesPanel.Visibility == Visibility.Collapsed)
                {
                    facilitiesPanel.Visibility = Visibility.Visible;
                    button.Content = "Sembunyikan detail fasilitas ↑";
                }
                else
                {
                    facilitiesPanel.Visibility = Visibility.Collapsed;
                    button.Content = "Tampilkan detail fasilitas ↓";
                }
            }
        }
<<<<<<< Updated upstream
=======

        // Helper method to find parent element
        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is T))
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        // Helper method to find child element by name
        private T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && (string.IsNullOrEmpty(childName) || typedChild.Name == childName))
                {
                    return typedChild;
                }

                var foundChild = FindChild<T>(child, childName);
                if (foundChild != null) return foundChild;
            }

            return null;
        }
>>>>>>> Stashed changes
    }
}