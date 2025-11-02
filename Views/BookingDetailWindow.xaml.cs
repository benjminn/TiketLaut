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
    public partial class BookingDetailWindow : Window
    {
        private ScheduleItem? _selectedSchedule;
        private bool _isFromSchedule;

        public BookingDetailWindow()
        {
            InitializeComponent();
            ApplyResponsiveLayout();
            
            // NavbarSelainHomepage tidak memerlukan SetUserInfo karena hanya menampilkan logo
        }

        // Constructor untuk mengetahui apakah berasal dari schedule
        public BookingDetailWindow(bool isFromSchedule = false) : this()
        {
            _isFromSchedule = isFromSchedule;
        }

        public void SetScheduleData(ScheduleItem schedule)
        {
            _selectedSchedule = schedule;
            UpdateUIWithScheduleData();
        }

        private void UpdateUIWithScheduleData()
        {
            if (_selectedSchedule == null) return;

            // Update the schedule information in the right sidebar
            // You may need to find the TextBlocks in the sidebar and update them
            // For now, this is a placeholder - you'll need to implement based on your XAML structure

            // Example: Update ferry type, route, date, price etc.
            // These would correspond to TextBlocks in your XAML with proper names
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            if (this.IsLoaded && MainContentGrid != null)
            {
                double windowWidth = this.ActualWidth;

                if (windowWidth < 1400)
                {
                    MainContentGrid.Margin = new Thickness(60, 20, 60, 40);
                }
                else if (windowWidth < 1600)
                {
                    MainContentGrid.Margin = new Thickness(80, 25, 80, 45);
                }
                else
                {
                    MainContentGrid.Margin = new Thickness(95, 30, 95, 50);
                }
            }
        }

        // TextBox Placeholder Logic
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string placeholder = textBox.Tag?.ToString() ?? "";

                // Jika text saat ini sama dengan placeholder, hapus dan ubah warna
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string placeholder = textBox.Tag?.ToString() ?? "";

                // Jika TextBox kosong saat kehilangan focus, kembalikan placeholder
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                }
            }
        }

        private bool IsPlaceholderText(TextBox textBox)
        {
            string placeholder = textBox.Tag?.ToString() ?? "";
            return textBox.Text == placeholder;
        }

        // Floating Label Logic
        private void FloatingTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    // Find the label's parent Border to change its VerticalAlignment
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Top;
                        labelBorder.Margin = new Thickness(12, 8, 0, 0);
                    }
                    
                    // Animate label to float up
                    label.FontSize = 11;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));
                    
                    // Adjust TextBox padding when focused
                    textBox.Padding = new Thickness(16, 16, 16, 8);
                }
            }
        }

        private void FloatingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    // Find the label's parent Border to reset its VerticalAlignment
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Center;
                        labelBorder.Margin = new Thickness(12, 0, 0, 0);
                    }
                    
                    // Reset label if textbox is empty
                    label.FontSize = 14;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                    
                    // Reset TextBox padding
                    textBox.Padding = new Thickness(16, 0, 16, 0);
                }
            }
        }

        private void FloatingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;
                    
                    // Keep label floated if there's text
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Top;
                            labelBorder.Margin = new Thickness(12, 8, 0, 0);
                        }
                        label.FontSize = 11;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));
                        textBox.Padding = new Thickness(16, 16, 16, 8);
                    }
                    else if (!textBox.IsFocused)
                    {
                        // Reset only if not focused and empty
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Center;
                            labelBorder.Margin = new Thickness(12, 0, 0, 0);
                        }
                        label.FontSize = 14;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                        textBox.Padding = new Thickness(16, 0, 16, 0);
                    }
                }
            }
        }

        // Floating Label Logic for ComboBox
        private void FloatingComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Top;
                        labelBorder.Margin = new Thickness(12, 8, 0, 0);
                    }
                    
                    label.FontSize = 11;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));
                    comboBox.Padding = new Thickness(16, 16, 16, 8);
                }
            }
        }

        private void FloatingComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null && comboBox.SelectedIndex == -1)
                {
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Center;
                        labelBorder.Margin = new Thickness(12, 0, 0, 0);
                    }
                    
                    label.FontSize = 14;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                    comboBox.Padding = new Thickness(16, 0, 16, 0);
                }
            }
        }

        private void FloatingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;
                    
                    if (comboBox.SelectedIndex != -1)
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Top;
                            labelBorder.Margin = new Thickness(12, 8, 0, 0);
                        }
                        label.FontSize = 11;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));
                        comboBox.Padding = new Thickness(16, 16, 16, 8);
                    }
                    else if (!comboBox.IsFocused)
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Center;
                            labelBorder.Margin = new Thickness(12, 0, 0, 0);
                        }
                        label.FontSize = 14;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                        comboBox.Padding = new Thickness(16, 0, 16, 0);
                    }
                }
            }
        }

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            // Kembali ke ScheduleWindow jika dari schedule, atau ke HomePage
            if (_isFromSchedule)
            {
                var scheduleWindow = new ScheduleWindow();
                scheduleWindow.Left = this.Left;
                scheduleWindow.Top = this.Top;
                scheduleWindow.Width = this.Width;
                scheduleWindow.Height = this.Height;
                scheduleWindow.WindowState = this.WindowState;
                scheduleWindow.Show();
                this.Close();
            }
            else
            {
                var homePage = new HomePage();
                homePage.Left = this.Left;
                homePage.Top = this.Top;
                homePage.Width = this.Width;
                homePage.Height = this.Height;
                homePage.WindowState = this.WindowState;
                homePage.Show();
                this.Close();
            }
        }

        private void BtnTogglePassenger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            string passengerNumber = button.Tag?.ToString() ?? "1";

            // Find the panel and image icon
            var panel = this.FindName($"pnlPassenger{passengerNumber}") as StackPanel;
            var image = this.FindName($"pathTogglePassenger{passengerNumber}") as Image;

            if (panel != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    panel.Visibility = Visibility.Visible;
                    // Rotate arrow down (180 degrees) for up-down rotation
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    // Rotate arrow up (0 degrees)
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }
            }
        }

        private void BtnToggleDetailHarga_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the detail harga panel
            var panel = this.FindName("pnlDetailHarga") as StackPanel;
            var image = this.FindName("pathToggleDetailHarga") as Image;
            var txtHeader = this.FindName("txtHeaderHarga") as TextBlock;
            var txtPrice = this.FindName("txtTotalHargaCollapsed") as TextBlock;
            var borderSeparator = this.FindName("borderSeparatorCollapsed") as Border;
            
            if (panel != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    // Expand: Show detail
                    panel.Visibility = Visibility.Visible;
                    if (txtHeader != null) txtHeader.Text = "Detail Harga";
                    if (txtPrice != null) txtPrice.Visibility = Visibility.Collapsed;
                    if (borderSeparator != null) borderSeparator.Visibility = Visibility.Collapsed;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    // Collapse: Show total
                    panel.Visibility = Visibility.Collapsed;
                    if (txtHeader != null) txtHeader.Text = "Total Harga";
                    if (txtPrice != null) txtPrice.Visibility = Visibility.Visible;
                    if (borderSeparator != null) borderSeparator.Visibility = Visibility.Visible;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }
            }
        }

        private void BtnToggleSidebarHarga_Click(object sender, RoutedEventArgs e)
        {
            var panel = FindName("pnlSidebarDetailHarga") as StackPanel;
            var image = FindName("pathToggleSidebarHarga") as Image;

            if (panel != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    // EXPAND
                    panel.Visibility = Visibility.Visible;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    // COLLAPSE
                    panel.Visibility = Visibility.Collapsed;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }
            }
        }

        private void ToggleDetailHarga(StackPanel panel, System.Windows.Shapes.Path path)
        {
            if (panel != null && path != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    panel.Visibility = Visibility.Visible;
                    if (path.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    if (path.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }
            }
        }

        private async void BtnLanjutPembayaran_Click(object sender, RoutedEventArgs e)
        {
            // Validate Detail Pemesan
            if (IsPlaceholderText(txtNamaPemesan) || string.IsNullOrWhiteSpace(txtNamaPemesan.Text))
            {
                MessageBox.Show("Nama pemesan harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNamaPemesan.Focus();
                return;
            }

            if (IsPlaceholderText(txtNomorPonsel) || string.IsNullOrWhiteSpace(txtNomorPonsel.Text))
            {
                MessageBox.Show("Nomor ponsel harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNomorPonsel.Focus();
                return;
            }

            if (IsPlaceholderText(txtEmail) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Email harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            // Validate Detail Penumpang (basic validation for at least one passenger)
            if (IsPlaceholderText(txtNamaPassenger1) || string.IsNullOrWhiteSpace(txtNamaPassenger1.Text))
            {
                MessageBox.Show("Data penumpang 1 harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                if (pnlPassenger1.Visibility == Visibility.Collapsed)
                {
                    pnlPassenger1.Visibility = Visibility.Visible;
                    if (pathTogglePassenger1.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 90;
                    }
                }
                txtNamaPassenger1.Focus();
                return;
            }

            if (IsPlaceholderText(txtIdPassenger1) || string.IsNullOrWhiteSpace(txtIdPassenger1.Text))
            {
                MessageBox.Show("Nomor identitas penumpang 1 harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                if (pnlPassenger1.Visibility == Visibility.Collapsed)
                {
                    pnlPassenger1.Visibility = Visibility.Visible;
                    if (pathTogglePassenger1.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 90;
                    }
                }
                txtIdPassenger1.Focus();
                return;
            }

            // Validate Detail Kendaraan
            if (IsPlaceholderText(txtPlatNomor) || string.IsNullOrWhiteSpace(txtPlatNomor.Text))
            {
                MessageBox.Show("Plat nomor kendaraan harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPlatNomor.Focus();
                return;
            }

            // ============ KODE BARU: SIMPAN KE DATABASE ============
            try
            {
                // Show loading
                btnLanjutPembayaran.IsEnabled = false;
                btnLanjutPembayaran.Content = "Memproses...";

                // Validasi session user
                if (TiketLaut.Services.SessionManager.CurrentUser == null)
                {
                    MessageBox.Show("Sesi login Anda telah berakhir. Silakan login kembali.", 
                        "Session Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi data schedule dan search criteria
                if (_selectedSchedule == null || _searchCriteria == null)
                {
                    MessageBox.Show("Data jadwal tidak lengkap. Silakan ulangi pemesanan dari awal.", 
                        "Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ambil data dari form
                var bookingData = new TiketLaut.Services.BookingData
                {
                    PenggunaId = TiketLaut.Services.SessionManager.CurrentUser.pengguna_id,
                    JadwalId = _selectedSchedule.JadwalId,
                    JenisKendaraanId = _searchCriteria.JenisKendaraanId,
                    JumlahPenumpang = _searchCriteria.JumlahPenumpang,
                    PlatNomor = vehicleSection?.Visibility == Visibility.Visible ? txtPlatNomor?.Text : null,
                    DataPenumpang = new List<TiketLaut.Services.PenumpangData>()
                };

                // Ambil data penumpang dari form (maksimal 3 penumpang sesuai UI)
                for (int i = 1; i <= Math.Min(_searchCriteria.JumlahPenumpang, 3); i++)
                {
                    var txtNama = FindName($"txtNamaPassenger{i}") as TextBox;
                    var txtId = FindName($"txtIdPassenger{i}") as TextBox;
                    var cmbJenisIdentitas = FindName($"cmbJenisIdentitasPassenger{i}") as ComboBox;
                    var cmbJenisKelamin = FindName($"cmbJenisKelaminPassenger{i}") as ComboBox;

                    if (txtNama != null && txtId != null && 
                        !string.IsNullOrWhiteSpace(txtNama.Text) && 
                        !IsPlaceholderText(txtNama) &&
                        !string.IsNullOrWhiteSpace(txtId.Text) &&
                        !IsPlaceholderText(txtId))
                    {
                        // Parse nomor identitas
                        if (!long.TryParse(txtId.Text.Trim(), out long nomorIdentitas))
                        {
                            MessageBox.Show($"Nomor identitas penumpang {i} tidak valid!", 
                                "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtId.Focus();
                            return;
                        }

                        var penumpangData = new TiketLaut.Services.PenumpangData
                        {
                            Nama = txtNama.Text.Trim(),
                            NomorIdentitas = nomorIdentitas,
                            JenisIdentitas = (cmbJenisIdentitas?.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "KTP",
                            JenisKelamin = (cmbJenisKelamin?.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Laki-laki"
                        };

                        bookingData.DataPenumpang.Add(penumpangData);
                    }
                }

                // Validasi minimal ada 1 penumpang
                if (bookingData.DataPenumpang.Count == 0)
                {
                    MessageBox.Show("Data penumpang harus diisi minimal 1 orang!", 
                        "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Simpan booking ke database
                var bookingService = new TiketLaut.Services.BookingService();
                var tiket = await bookingService.CreateBookingAsync(bookingData);

                MessageBox.Show(
                    $"? Booking berhasil!\n\n" +
                    $"Kode Tiket: {tiket.kode_tiket}\n" +
                    $"Total: Rp {tiket.total_harga:N0}\n\n" +
                    $"Silakan lanjutkan ke pembayaran.",
                    "Booking Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // NEW: Pass tiket data ke PaymentWindow
                var paymentWindow = new PaymentWindow();
                paymentWindow.SetTiketData(tiket.tiket_id, tiket.total_harga);  // <-- TAMBAHKAN INI
                paymentWindow.Left = this.Left;
                paymentWindow.Top = this.Top;
                paymentWindow.Width = this.Width;
                paymentWindow.Height = this.Height;
                paymentWindow.WindowState = this.WindowState;
                paymentWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"? Terjadi kesalahan:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btnLanjutPembayaran.IsEnabled = true;
                btnLanjutPembayaran.Content = "Lanjut Pembayaran";
            }
        }
    }
}

