using System.Windows;
using TiketLaut.Views;

namespace TiketLaut.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Window
    {
        private bool _isLoggedIn = false;
        private string _currentUser = "";

        // Constructor default (untuk pertama kali buka app)
        public HomePage()
        {
            InitializeComponent();
            SetNavbarVisibility();
        }

        // Constructor dengan parameter (untuk setelah login/logout)
        public HomePage(bool isLoggedIn, string username = "") : this()
        {
            _isLoggedIn = isLoggedIn;
            _currentUser = username;

            SetNavbarVisibility();

            if (_isLoggedIn && !string.IsNullOrEmpty(_currentUser))
            {
                navbarPostLogin.SetUserInfo(_currentUser);
            }
        }

        private void SetNavbarVisibility()
        {
            if (_isLoggedIn)
            {
                navbarPreLogin.Visibility = Visibility.Collapsed;
                navbarPostLogin.Visibility = Visibility.Visible;
            }
            else
            {
                navbarPreLogin.Visibility = Visibility.Visible;
                navbarPostLogin.Visibility = Visibility.Collapsed;
            }
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

            // Jika semua validasi berhasil, buka ScheduleWindow
            ScheduleWindow scheduleWindow = new ScheduleWindow();
            scheduleWindow.Show();
            this.Close();
        }

        private void cmbJenisKendaraan_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void navbarPreLogin_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
