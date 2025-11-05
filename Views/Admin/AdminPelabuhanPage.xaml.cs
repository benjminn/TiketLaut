using System;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminPelabuhanPage : UserControl
    {
        private readonly PelabuhanService _pelabuhanService;

        public AdminPelabuhanPage()
        {
            InitializeComponent();
            _pelabuhanService = new PelabuhanService();
            LoadPelabuhanData();
        }

        private async void LoadPelabuhanData()
        {
            try
            {
                var pelabuhans = await _pelabuhanService.GetAllPelabuhanAsync();
                dgPelabuhan.ItemsSource = pelabuhans;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddPelabuhan_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AdminPelabuhanFormDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadPelabuhanData();
            }
        }

        private void BtnDetailPelabuhan_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int pelabuhanId)
            {
                var dialog = new AdminPelabuhanDetailDialog(pelabuhanId)
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
            }
        }

        private async void BtnEditPelabuhan_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int pelabuhanId)
            {
                var pelabuhan = await _pelabuhanService.GetPelabuhanByIdAsync(pelabuhanId);
                if (pelabuhan != null)
                {
                    var dialog = new AdminPelabuhanFormDialog(pelabuhan);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadPelabuhanData();
                    }
                }
            }
        }

        private async void BtnDeletePelabuhan_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int pelabuhanId)
            {
                var result = MessageBox.Show(
                    "Apakah Anda yakin ingin menghapus pelabuhan ini?",
                    "Konfirmasi Hapus",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = await _pelabuhanService.DeletePelabuhanAsync(pelabuhanId);
                    MessageBox.Show(message, success ? "Success" : "Error",
                        MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (success)
                    {
                        LoadPelabuhanData();
                    }
                }
            }
        }
    }
}
