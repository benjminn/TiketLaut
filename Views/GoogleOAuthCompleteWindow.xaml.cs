using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TiketLaut.Views.Components;

namespace TiketLaut.Views
{
    public partial class GoogleOAuthCompleteWindow : Window
    {
        public string NamaLengkap { get; private set; } = string.Empty;
        public string NIK { get; private set; } = string.Empty;
        public DateOnly TanggalLahir { get; private set; }
        public bool IsCompleted { get; private set; } = false;

        private readonly string _googleEmail;
        public DateTime MaxDate { get; private set; }

        public GoogleOAuthCompleteWindow(string googleEmail, string googleName)
        {
            InitializeComponent();
            _googleEmail = googleEmail;
            
            // Set maksimal tanggal lahir (17 tahun yang lalu dari hari ini)
            MaxDate = DateTime.Today.AddYears(-17);
            dpTanggalLahir.DisplayDateEnd = MaxDate;
            
            // Event handler untuk update display ketika tanggal dipilih
            dpTanggalLahir.SelectedDateChanged += (s, e) =>
            {
                if (dpTanggalLahir.SelectedDate.HasValue)
                {
                    txtTanggalLahir.Text = dpTanggalLahir.SelectedDate.Value.ToString("dddd, dd MMMM yyyy");
                    txtTanggalLahir.Foreground = System.Windows.Media.Brushes.Black;
                }
            };
            
            // Set DataContext untuk binding
            DataContext = this;
            
            // Pre-fill nama lengkap dari Google jika ada
            if (!string.IsNullOrEmpty(googleName))
            {
                txtNamaLengkap.Text = googleName;
                txtWelcome.Text = $"Selamat datang, {googleName}!";
            }
        }

        // Event handler untuk validasi input NIK (hanya angka)
        private void TxtNIK_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Hanya izinkan angka 0-9
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Event handler untuk membuka kalender
        private void BtnCalendar_Click(object sender, RoutedEventArgs e)
        {
            dpTanggalLahir.IsDropDownOpen = true;
        }

        private void BtnSimpan_Click(object sender, RoutedEventArgs e)
        {
            // Validasi input
            string namaLengkap = txtNamaLengkap.Text.Trim();
            string nik = txtNIK.Text.Trim();

            if (string.IsNullOrEmpty(namaLengkap))
            {
                CustomDialog.ShowWarning("Validasi Error", "Nama lengkap tidak boleh kosong!");
                txtNamaLengkap.Focus();
                return;
            }

            if (string.IsNullOrEmpty(nik))
            {
                CustomDialog.ShowWarning("Validasi Error", "NIK tidak boleh kosong!");
                txtNIK.Focus();
                return;
            }

            // Validasi NIK harus 16 digit angka
            if (nik.Length != 16 || !long.TryParse(nik, out _))
            {
                CustomDialog.ShowWarning("Validasi Error", "NIK harus berupa 16 digit angka!");
                txtNIK.Focus();
                return;
            }

            if (!dpTanggalLahir.SelectedDate.HasValue)
            {
                CustomDialog.ShowWarning("Validasi Error", "Tanggal lahir tidak boleh kosong!");
                dpTanggalLahir.Focus();
                return;
            }

            // Validasi umur minimal 17 tahun
            var tanggalLahir = dpTanggalLahir.SelectedDate.Value;
            var umur = DateTime.Today.Year - tanggalLahir.Year;
            if (DateTime.Today < tanggalLahir.AddYears(umur))
                umur--;

            if (umur < 17)
            {
                CustomDialog.ShowWarning("Validasi Error", "Usia minimal untuk registrasi adalah 17 tahun!");
                dpTanggalLahir.Focus();
                return;
            }

            // Set properties
            NamaLengkap = namaLengkap;
            NIK = nik;
            TanggalLahir = DateOnly.FromDateTime(tanggalLahir);
            IsCompleted = true;

            // Close dialog dengan result OK
            DialogResult = true;
            Close();
        }

        private void BtnBatal_Click(object sender, RoutedEventArgs e)
        {
            IsCompleted = false;
            DialogResult = false;
            Close();
        }
    }
}
