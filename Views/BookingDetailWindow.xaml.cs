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

                if (windowWidth < 1280)
                {
                    MainContentGrid.Margin = new Thickness(20, 15, 20, 20);
                }
                else if (windowWidth < 1600)
                {
                    MainContentGrid.Margin = new Thickness(30, 20, 30, 25);
                }
                else
                {
                    MainContentGrid.Margin = new Thickness(40, 20, 40, 30);
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

            // Find the panel and path
            var panel = this.FindName($"pnlPassenger{passengerNumber}") as StackPanel;
            var path = this.FindName($"pathTogglePassenger{passengerNumber}") as System.Windows.Shapes.Path;

            if (panel != null && path != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    panel.Visibility = Visibility.Visible;
                    // Rotate arrow down (90 degrees)
                    if (path.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 90;
                    }
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    // Rotate arrow right (0 degrees)
                    if (path.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }
            }
        }

        private void BtnToggleDetailHarga_Click(object sender, RoutedEventArgs e)
        {
            // Toggle only the main detail harga panel since sidebar doesn't have a collapsible version
            ToggleDetailHarga(pnlDetailHarga, pathToggleDetailHarga);
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
                        rotate.Angle = 90;
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

        private void BtnLanjutPembayaran_Click(object sender, RoutedEventArgs e)
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

                // Open passenger 1 panel if collapsed
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

                // Open passenger 1 panel if collapsed
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

            // All validations passed, navigate to Payment
            var paymentWindow = new PaymentWindow();

            // Pass schedule data to payment window if needed
            if (_selectedSchedule != null)
            {
                // You can add a SetScheduleData method to PaymentWindow as well
                // paymentWindow.SetScheduleData(_selectedSchedule);
            }

            paymentWindow.Left = this.Left;
            paymentWindow.Top = this.Top;
            paymentWindow.Width = this.Width;
            paymentWindow.Height = this.Height;
            paymentWindow.WindowState = this.WindowState;
            paymentWindow.Show();
            this.Close();
        }
    }
}

