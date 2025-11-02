using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace TiketLaut.Views
{
    /// <summary>
    /// Interaction logic for PaymentWindow.xaml
    /// </summary>
    public partial class PaymentWindow : Window
    {
        private string selectedPaymentMethod = "";
        private bool isPaymentMethodSelected = false;
        private int kodeUnik = 0; // Kode unik yang ditambahkan untuk transfer bank
        private int hargaAsli = 487000; // Harga asli sebelum ditambah kode unik

        public PaymentWindow()
        {
            InitializeComponent();
            ApplyResponsiveLayout();
            GenerateKodeUnik();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            double windowWidth = this.ActualWidth;

            // Adjust margins based on window width
            if (windowWidth < 1280)
            {
                // Small screens
                MainContentGrid.Margin = new Thickness(20, 15, 20, 20);
            }
            else if (windowWidth < 1600)
            {
                // Medium screens
                MainContentGrid.Margin = new Thickness(30, 20, 30, 25);
            }
            else
            {
                // Large screens
                MainContentGrid.Margin = new Thickness(40, 20, 40, 30);
            }
        }

        private void PaymentMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                selectedPaymentMethod = rb.Tag.ToString();
                isPaymentMethodSelected = true;

                // Update button text
                if (txtMainActionButton != null)
                {
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";
                }

                // Auto-check parent radio button based on selection
                if (selectedPaymentMethod == "BCA" || selectedPaymentMethod == "Mandiri")
                {
                    // Transfer Bank selected - check parent
                    if (rbTransferBank != null)
                    {
                        rbTransferBank.IsChecked = true;
                    }
                }
                else if (selectedPaymentMethod == "Indomaret" || selectedPaymentMethod == "Alfamart")
                {
                    // Gerai Retail selected - check parent
                    if (rbGeraiRetail != null)
                    {
                        rbGeraiRetail.IsChecked = true;
                    }
                }
                else
                {
                    // QRIS or Kartu Kredit - uncheck parent radio buttons
                    if (rbTransferBank != null) rbTransferBank.IsChecked = false;
                    if (rbGeraiRetail != null) rbGeraiRetail.IsChecked = false;
                }

                // Update bank info based on selection
                UpdateBankInfo(selectedPaymentMethod);
            }
        }

        private void PaymentMethod_Unchecked(object sender, RoutedEventArgs e)
        {
            // Optional: Handle unchecked state if needed
        }

        private void UpdateBankInfo(string paymentMethod)
        {
            if (txtBankName == null || imgSelectedBank == null) return;

            bool isTransferBank = (paymentMethod == "BCA" || paymentMethod == "Mandiri");

            switch (paymentMethod)
            {
                case "BCA":
                    txtBankName.Text = "Bank BCA";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/BCA.png", UriKind.Relative));
                    txtAccountNumber.Text = "385 0833 817";
                    break;
                case "Mandiri":
                    txtBankName.Text = "Bank Mandiri";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Mandiri.png", UriKind.Relative));
                    txtAccountNumber.Text = "142 0099 123456";
                    break;
                case "Indomaret":
                    txtBankName.Text = "Indomaret";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Indomaret.png", UriKind.Relative));
                    txtAccountNumber.Text = "Kode: INDOM12345";
                    break;
                case "Alfamart":
                    txtBankName.Text = "Alfamart";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Alfamart.png", UriKind.Relative));
                    txtAccountNumber.Text = "Kode: ALFA67890";
                    break;
                default:
                    txtBankName.Text = "Metode Pembayaran";
                    txtAccountNumber.Text = "-";
                    break;
            }

            // Update visibility kode unik - hanya tampil untuk transfer bank
            if (pnlDetailPembayaran != null && pnlDetailPembayaran.Visibility == Visibility.Visible)
            {
                if (txtKodeUnik != null)
                {
                    txtKodeUnik.Visibility = isTransferBank ? Visibility.Visible : Visibility.Collapsed;
                    if (isTransferBank)
                    {
                        txtKodeUnik.Text = $"Kode unik (+{kodeUnik}) pada 3 digit terakhir";
                    }
                }
            }
        }

        private void BtnMainAction_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaymentMethodSelected)
            {
                MessageBox.Show(
                    "Silakan pilih metode pembayaran terlebih dahulu!",
                    "Informasi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (PaymentMethodsCard.Visibility == Visibility.Visible)
            {
                // Show payment instructions
                PaymentMethodsCard.Visibility = Visibility.Collapsed;
                PaymentInstructionsCard.Visibility = Visibility.Visible;
                txtMainActionButton.Text = "Cek Status Pembayaran";
            }
            else
            {
                // Check payment status
                MessageBox.Show(
                    "Fitur cek status pembayaran akan segera tersedia!",
                    "Informasi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnGantiMetode_Click(object sender, RoutedEventArgs e)
        {
            // Switch back to payment methods
            PaymentInstructionsCard.Visibility = Visibility.Collapsed;
            PaymentMethodsCard.Visibility = Visibility.Visible;
            txtMainActionButton.Text = "Konfirmasi Pembayaran";
        }

        private void BtnCaraMembayar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"Cara pembayaran untuk {selectedPaymentMethod}:\n\n" +
                "1. Buka aplikasi mobile banking atau ATM\n" +
                "2. Pilih menu Transfer\n" +
                "3. Masukkan nomor rekening tujuan\n" +
                "4. Masukkan nominal sesuai yang tertera\n" +
                "5. Konfirmasi transaksi\n" +
                "6. Simpan bukti transfer",
                "Cara Membayar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnCopyAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAccountNumber.Text))
            {
                Clipboard.SetText(txtAccountNumber.Text);
                MessageBox.Show(
                    "Nomor rekening telah disalin!",
                    "Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnCopyAmount_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("487123");
            MessageBox.Show(
                "Jumlah pembayaran telah disalin!",
                "Berhasil",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void GenerateKodeUnik()
        {
            // Generate kode unik random antara 1-999
            Random random = new Random();
            kodeUnik = random.Next(1, 1000);
            
            // Update total pembayaran dengan kode unik
            UpdateTotalPembayaran();
        }

        private void UpdateTotalPembayaran()
        {
            int totalPembayaran = hargaAsli + kodeUnik;
            string totalString = totalPembayaran.ToString();
            string mainPart = totalString.Substring(0, totalString.Length - 3);
            string lastThreeDigits = totalString.Substring(totalString.Length - 3);

            // Update semua TextBlock total pembayaran
            if (txtTotalPembayaran != null)
            {
                txtTotalPembayaran.Text = $"IDR {mainPart}.";
                txtTotalPembayaranDigit.Text = lastThreeDigits;
            }
            
            if (txtDetailTotalPembayaran != null)
            {
                txtDetailTotalPembayaran.Text = $"IDR {mainPart}.";
                txtDetailTotalPembayaranDigit.Text = lastThreeDigits;
            }
        }

        private void BtnToggleDetailPembayaran_Click(object sender, RoutedEventArgs e)
        {
            if (pnlDetailPembayaran.Visibility == Visibility.Collapsed)
            {
                // Show detail
                pnlDetailPembayaran.Visibility = Visibility.Visible;
                
                // Rotate arrow icon
                var rotateTransform = imgToggleDetailPembayaran.RenderTransform as System.Windows.Media.RotateTransform;
                if (rotateTransform != null)
                {
                    var animation = new DoubleAnimation
                    {
                        To = 180,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
                }
                
                // Show kode unik notification if transfer bank is selected
                if (selectedPaymentMethod == "BCA" || selectedPaymentMethod == "Mandiri")
                {
                    if (txtKodeUnik != null)
                    {
                        txtKodeUnik.Visibility = Visibility.Visible;
                        txtKodeUnik.Text = $"Kode unik (+{kodeUnik}) pada 3 digit terakhir";
                    }
                }
            }
            else
            {
                // Hide detail
                pnlDetailPembayaran.Visibility = Visibility.Collapsed;
                
                // Rotate arrow icon back
                var rotateTransform = imgToggleDetailPembayaran.RenderTransform as System.Windows.Media.RotateTransform;
                if (rotateTransform != null)
                {
                    var animation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
                }
            }
        }

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Apakah Anda yakin membatalkan pembayaran?",
                "Konfirmasi Pembatalan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Navigate to ScheduleWindow
                var scheduleWindow = new ScheduleWindow();
                scheduleWindow.Left = this.Left;
                scheduleWindow.Top = this.Top;
                scheduleWindow.Width = this.Width;
                scheduleWindow.Height = this.Height;
                scheduleWindow.WindowState = this.WindowState;
                scheduleWindow.Show();
                this.Close();
            }
            // If user clicks "No", stay on current window (do nothing)
        }


        // Method untuk membuka PaymentWindow dari window lain
        public static void Open()
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.Show();
        }
    }
}