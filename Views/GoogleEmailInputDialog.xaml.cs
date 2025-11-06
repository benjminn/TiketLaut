using System.Windows;

namespace TiketLaut.Views
{
    public partial class GoogleEmailInputDialog : Window
    {
        public string GoogleEmail { get; private set; } = string.Empty;
        public string GoogleName { get; private set; } = string.Empty;

        public GoogleEmailInputDialog()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            GoogleEmail = txtEmail.Text.Trim();
            GoogleName = txtName.Text.Trim();

            if (string.IsNullOrWhiteSpace(GoogleEmail))
            {
                MessageBox.Show("Email tidak boleh kosong!",
                               "Validasi Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(GoogleName))
            {
                MessageBox.Show("Nama tidak boleh kosong!",
                               "Validasi Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
