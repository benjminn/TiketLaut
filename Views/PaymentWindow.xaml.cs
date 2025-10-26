using System;
using System.Windows;
using System.Windows.Controls;

namespace TiketLaut.Views
{
    /// <summary>
    /// Interaction logic for PaymentWindow.xaml
    /// </summary>
    public partial class PaymentWindow : Window
    {
        private string selectedPaymentMethod = "";
        private bool isPaymentMethodSelected = false;

        public PaymentWindow()
        {
            InitializeComponent();
            ApplyResponsiveLayout();
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

        // Method untuk membuka PaymentWindow dari window lain
        public static void Open()
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.Show();
        }
    }
}