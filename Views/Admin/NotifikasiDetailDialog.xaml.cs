using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Data;

namespace TiketLaut.Views.Admin
{
    public partial class NotifikasiDetailDialog : Window
    {
        private readonly Notifikasi _notifikasi;
        private readonly AppDbContext _context;

        public NotifikasiDetailDialog(Notifikasi notifikasi, AppDbContext context)
        {
            InitializeComponent();
            _notifikasi = notifikasi;
            _context = context;
            LoadNotifikasiDetail();
        }

        private void LoadNotifikasiDetail()
        {
            try
            {
                // Load data notifikasi dengan include relasi
                var notif = _context.Notifikasis
                    .Include(n => n.Pengguna)
                    .Include(n => n.Admin)
                    .FirstOrDefault(n => n.notifikasi_id == _notifikasi.notifikasi_id);

                if (notif == null)
                {
                    MessageBox.Show("Notifikasi tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Header
                txtNotifId.Text = $"ID: #{notif.notifikasi_id}";

                // Informasi Penerima
                txtPengguna.Text = notif.Pengguna?.nama ?? "Tidak diketahui";
                txtPenggunaId.Text = notif.pengguna_id.ToString();

                // Informasi Notifikasi
                txtJenis.Text = notif.jenis_notifikasi;
                txtJudul.Text = notif.judul_notifikasi;
                txtPesan.Text = notif.pesan;
                txtStatusBaca.Text = notif.status_baca ? "✓ Sudah dibaca" : "✗ Belum dibaca";
                txtStatusBaca.Foreground = notif.status_baca ? 
                    System.Windows.Media.Brushes.Green : 
                    System.Windows.Media.Brushes.Red;

                // Informasi Pengirim
                if (notif.oleh_system)
                {
                    txtSumber.Text = "🤖 Otomatis (Sistem)";
                    lblAdmin.Visibility = Visibility.Collapsed;
                    txtAdmin.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtSumber.Text = "👨‍💼 Manual (Admin)";
                    if (notif.Admin != null)
                    {
                        lblAdmin.Visibility = Visibility.Visible;
                        txtAdmin.Visibility = Visibility.Visible;
                        txtAdmin.Text = $"{notif.Admin.nama} ({notif.Admin.email})";
                    }
                    else
                    {
                        lblAdmin.Visibility = Visibility.Visible;
                        txtAdmin.Visibility = Visibility.Visible;
                        txtAdmin.Text = "Admin tidak diketahui";
                    }
                }

                txtWaktu.Text = notif.waktu_kirim.ToString("dddd, dd MMMM yyyy HH:mm:ss");

                // Update button state
                btnTandaiBaca.IsEnabled = !notif.status_baca;
                if (notif.status_baca)
                {
                    btnTandaiBaca.Content = "✓ Sudah Ditandai Dibaca";
                    btnTandaiBaca.Opacity = 0.6;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error memuat detail notifikasi: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void BtnTandaiBaca_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var notif = _context.Notifikasis.Find(_notifikasi.notifikasi_id);
                if (notif != null && !notif.status_baca)
                {
                    notif.status_baca = true;
                    _context.SaveChanges();

                    MessageBox.Show("Notifikasi berhasil ditandai sebagai sudah dibaca!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Update tampilan
                    txtStatusBaca.Text = "✓ Sudah dibaca";
                    txtStatusBaca.Foreground = System.Windows.Media.Brushes.Green;
                    btnTandaiBaca.IsEnabled = false;
                    btnTandaiBaca.Content = "✓ Sudah Ditandai Dibaca";
                    btnTandaiBaca.Opacity = 0.6;

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error menandai notifikasi: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnTutup_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
