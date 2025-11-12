using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminKapalFormDialog : Window
    {
        private readonly KapalService _kapalService;
        private Kapal? _existingKapal;
        private bool _isEditMode;

        public AdminKapalFormDialog(Kapal? kapal = null)
        {
            InitializeComponent();
            _kapalService = new KapalService();
            _existingKapal = kapal;
            _isEditMode = kapal != null;

            if (_isEditMode && kapal != null)
            {
                txtTitle.Text = "Edit Kapal";
                btnSave.Content = "Update";
                LoadKapalData(kapal);
            }
        }
        
        // Event handler untuk validasi input angka
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Hanya izinkan angka (0-9)
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void LoadKapalData(Kapal kapal)
        {
            txtNamaKapal.Text = kapal.nama_kapal;
            txtKapasitasPenumpang.Text = kapal.kapasitas_penumpang_max.ToString();
            txtKapasitasKendaraan.Text = kapal.kapasitas_kendaraan_max.ToString();
            txtFasilitas.Text = kapal.fasilitas;
            txtDeskripsi.Text = kapal.deskripsi ?? "";
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validasi
            if (string.IsNullOrWhiteSpace(txtNamaKapal.Text))
            {
                MessageBox.Show("Nama kapal harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtKapasitasPenumpang.Text, out int kapasitasPenumpang) || kapasitasPenumpang <= 0)
            {
                MessageBox.Show("Kapasitas penumpang harus berupa angka positif!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtKapasitasKendaraan.Text, out int kapasitasKendaraan) || kapasitasKendaraan <= 0)
            {
                MessageBox.Show("Kapasitas kendaraan harus berupa angka positif!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtFasilitas.Text))
            {
                MessageBox.Show("Fasilitas harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable button
            btnSave.IsEnabled = false;
            btnSave.Content = "Menyimpan...";

            try
            {
                var kapal = new Kapal
                {
                    nama_kapal = txtNamaKapal.Text.Trim(),
                    kapasitas_penumpang_max = kapasitasPenumpang,
                    kapasitas_kendaraan_max = kapasitasKendaraan,
                    fasilitas = txtFasilitas.Text.Trim(),
                    deskripsi = string.IsNullOrWhiteSpace(txtDeskripsi.Text) ? null : txtDeskripsi.Text.Trim()
                };

                (bool success, string message) result;

                if (_isEditMode && _existingKapal != null)
                {
                    kapal.kapal_id = _existingKapal.kapal_id;
                    result = await _kapalService.UpdateKapalAsync(kapal);
                }
                else
                {
                    result = await _kapalService.CreateKapalAsync(kapal);
                }

                MessageBox.Show(result.message, result.success ? "Success" : "Error",
                    MessageBoxButton.OK, result.success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (result.success)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSave.IsEnabled = true;
                btnSave.Content = _isEditMode ? "Update" : "Simpan";
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
