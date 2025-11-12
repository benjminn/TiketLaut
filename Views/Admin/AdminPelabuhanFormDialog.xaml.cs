using System;
using System.Windows;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminPelabuhanFormDialog : Window
    {
        private readonly PelabuhanService _pelabuhanService;
        private Pelabuhan? _existingPelabuhan;
        private bool _isEditMode;

        public AdminPelabuhanFormDialog(Pelabuhan? pelabuhan = null)
        {
            InitializeComponent();
            _pelabuhanService = new PelabuhanService();
            _existingPelabuhan = pelabuhan;
            _isEditMode = pelabuhan != null;

            if (_isEditMode && pelabuhan != null)
            {
                txtTitle.Text = "Edit Pelabuhan";
                btnSave.Content = "Update";
                LoadPelabuhanData(pelabuhan);
            }
        }

        private void LoadPelabuhanData(Pelabuhan pelabuhan)
        {
            txtNamaPelabuhan.Text = pelabuhan.nama_pelabuhan;
            txtKota.Text = pelabuhan.kota;
            txtProvinsi.Text = pelabuhan.provinsi;
            txtFasilitas.Text = pelabuhan.fasilitas;
            txtDeskripsi.Text = pelabuhan.deskripsi ?? "";
            
            // Set timezone
            foreach (System.Windows.Controls.ComboBoxItem item in cmbTimezone.Items)
            {
                if (item.Tag?.ToString() == pelabuhan.timezone)
                {
                    cmbTimezone.SelectedItem = item;
                    break;
                }
            }
            
            // Default to WIB if not found
            if (cmbTimezone.SelectedItem == null)
            {
                cmbTimezone.SelectedIndex = 0;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNamaPelabuhan.Text))
            {
                MessageBox.Show("Nama pelabuhan harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtKota.Text))
            {
                MessageBox.Show("Kota harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtProvinsi.Text))
            {
                MessageBox.Show("Provinsi harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtFasilitas.Text))
            {
                MessageBox.Show("Fasilitas harus diisi!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbTimezone.SelectedItem == null)
            {
                MessageBox.Show("Timezone harus dipilih!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable button
            btnSave.IsEnabled = false;
            btnSave.Content = "Menyimpan...";

            try
            {
                var selectedTimezone = ((System.Windows.Controls.ComboBoxItem)cmbTimezone.SelectedItem).Tag?.ToString() ?? "WIB";
                
                var pelabuhan = new Pelabuhan
                {
                    nama_pelabuhan = txtNamaPelabuhan.Text.Trim(),
                    kota = txtKota.Text.Trim(),
                    provinsi = txtProvinsi.Text.Trim(),
                    timezone = selectedTimezone,
                    fasilitas = txtFasilitas.Text.Trim(),
                    deskripsi = string.IsNullOrWhiteSpace(txtDeskripsi.Text) ? null : txtDeskripsi.Text.Trim()
                };

                (bool success, string message) result;

                if (_isEditMode && _existingPelabuhan != null)
                {
                    pelabuhan.pelabuhan_id = _existingPelabuhan.pelabuhan_id;
                    result = await _pelabuhanService.UpdatePelabuhanAsync(pelabuhan);
                }
                else
                {
                    result = await _pelabuhanService.CreatePelabuhanAsync(pelabuhan);
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
