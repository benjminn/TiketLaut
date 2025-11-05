using System;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminKapalPage : UserControl
    {
        private readonly KapalService _kapalService;

        public AdminKapalPage()
        {
            InitializeComponent();
            _kapalService = new KapalService();
            LoadKapalData();
        }

        private async void LoadKapalData()
        {
            try
            {
                var kapals = await _kapalService.GetAllKapalAsync();
                dgKapal.ItemsSource = kapals;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddKapal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AdminKapalFormDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadKapalData();
            }
        }

        private void BtnDetailKapal_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int kapalId)
            {
                var dialog = new AdminKapalDetailDialog(kapalId)
                {
                    Owner = Window.GetWindow(this)
                };
                dialog.ShowDialog();
            }
        }

        private async void BtnEditKapal_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int kapalId)
            {
                var kapal = await _kapalService.GetKapalByIdAsync(kapalId);
                if (kapal != null)
                {
                    var dialog = new AdminKapalFormDialog(kapal);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadKapalData();
                    }
                }
            }
        }

        private async void BtnDeleteKapal_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int kapalId)
            {
                var result = MessageBox.Show(
                    "Apakah Anda yakin ingin menghapus kapal ini?",
                    "Konfirmasi Hapus",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = await _kapalService.DeleteKapalAsync(kapalId);
                    MessageBox.Show(message, success ? "Success" : "Error",
                        MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (success)
                    {
                        LoadKapalData();
                    }
                }
            }
        }
    }
}
