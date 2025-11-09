using System.Windows;
using TiketLaut.Views.Components;

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
                CustomDialog.ShowWarning("Validasi Error", "Email tidak boleh kosong!");
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(GoogleName))
            {
                CustomDialog.ShowWarning("Validasi Error", "Nama tidak boleh kosong!");
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
