using System.Windows;
using TiketLaut.Views;

namespace TiketLaut.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Window
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Apakah Anda yakin ingin logout?", "Konfirmasi",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Buka LoginWindow
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                // Tutup HomePage
                this.Close();
            }
        }

        private void BtnCariJadwal_Click(object sender, RoutedEventArgs e)
        {
            // Validasi Pelabuhan Asal
            if (cmbPelabuhanAsal.SelectedIndex == 0)
            {
                MessageBox.Show("Silakan pilih Pelabuhan Asal!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validasi Kelas Layanan
            if (cmbKelasLayanan.SelectedIndex == 0)
            {
                MessageBox.Show("Silakan pilih Kelas Layanan!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

                // Validasi Tanggal
                if (!dpTanggal?.SelectedDate.HasValue ?? false)
                {
                    MessageBox.Show("Silakan pilih Tanggal!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Penumpang
                if (cmbPenumpang?.SelectedIndex == 0)
                {
                    MessageBox.Show("Silakan pilih Penumpang!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                    }

            if (cmbPelabuhanTujuan.SelectedIndex == 0)
            {
                MessageBox.Show("Silakan pilih Pelabuhan Tujuan!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbJenisKendaraan.SelectedIndex == 0)
            {
                MessageBox.Show("Silakan pilih Jenis Kendaraan!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbPenumpang.SelectedIndex == 0)
            {
                MessageBox.Show("Silakan pilih Penumpang!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDate = dpTanggal.SelectedDate?.ToString("dd/MM/yyyy");
            var selectedJam = cmbJam.SelectedItem?.ToString() ?? "Tidak dipilih";

            MessageBox.Show($"Pencarian jadwal dimulai...\nTanggal: {selectedDate}\nJam: {selectedJam}", "Sukses",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
