using System;
using System.Windows;
using System.Windows.Media;
using AdminModel = TiketLaut.Admin;

namespace TiketLaut.Views.Admin
{
    public partial class AdminDetailDialog : Window
    {
        public AdminDetailDialog(AdminModel admin)
        {
            InitializeComponent();
            LoadAdminData(admin);
        }

        private void LoadAdminData(AdminModel admin)
        {
            if (admin == null) return;

            txtID.Text = admin.admin_id.ToString();
            txtNama.Text = admin.nama;
            txtEmail.Text = admin.email;
            
            // Format role dengan warna berbeda
            if (admin.role == "0")
            {
                txtRole.Text = "⭐ Super Admin";
                borderRole.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3CD"));
                txtRole.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#856404"));
            }
            else
            {
                txtRole.Text = "Admin Operasional";
                borderRole.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1ECF1"));
                txtRole.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C5460"));
            }

            // Format tanggal
            txtCreatedAt.Text = admin.created_at?.ToString("dd MMMM yyyy HH:mm") ?? "Tidak tersedia";
            txtUpdatedAt.Text = admin.updated_at?.ToString("dd MMMM yyyy HH:mm") ?? "Tidak tersedia";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
