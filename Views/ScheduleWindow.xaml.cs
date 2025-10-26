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
                // Di sini bisa diarahkan ke halaman pembayaran
                MessageBox.Show("Mengarahkan ke halaman pembayaran...", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Contoh: Buka window pembayaran
                // PaymentWindow paymentWindow = new PaymentWindow();
                // paymentWindow.Show();
                // this.Close();
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
    }
}