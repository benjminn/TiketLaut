using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Data;
using TiketLaut.Services;

namespace TiketLaut.Views.Admin
{
    public partial class MarineWeatherPage : UserControl
    {
        private readonly MarineWeatherService _weatherService;

        public MarineWeatherPage()
        {
            InitializeComponent();
            _weatherService = new MarineWeatherService();
            
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                // Create new context for this operation
                using var context = DatabaseService.GetContext();
                
                // Load pelabuhan dengan koordinat
                var pelabuhanList = await context.Pelabuhans
                    .Where(p => p.latitude != null && p.longitude != null)
                    .OrderBy(p => p.nama_pelabuhan)
                    .ToListAsync();

                // Tambah info koordinat ke display
                var pelabuhanDisplay = pelabuhanList.Select(p => new
                {
                    Pelabuhan = p,
                    NamaPelabuhan = $"{p.nama_pelabuhan} ({p.kota})"
                }).ToList();

                cbPelabuhan.ItemsSource = pelabuhanDisplay;
                cbPelabuhanAsal.ItemsSource = pelabuhanDisplay;
                cbPelabuhanTujuan.ItemsSource = pelabuhanDisplay;

                // Load jadwal (5 hari ke depan saja)
                var jadwalList = await context.Jadwals
                    .Include(j => j.pelabuhan_asal)
                    .Include(j => j.pelabuhan_tujuan)
                    .Include(j => j.kapal)
                    .Where(j => j.waktu_berangkat >= DateTime.UtcNow && 
                               j.waktu_berangkat <= DateTime.UtcNow.AddDays(5))
                    .OrderBy(j => j.waktu_berangkat)
                    .ToListAsync();

                cbJadwal.ItemsSource = jadwalList;

                if (pelabuhanList.Count == 0)
                {
                    MessageBox.Show("⚠️ Belum ada data pelabuhan dengan koordinat!\n\n" +
                        "Silakan jalankan SQL script 'update_pelabuhan_coordinates.sql' terlebih dahulu.",
                        "Data Tidak Lengkap",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCheckPelabuhan_Click(object sender, RoutedEventArgs e)
        {
            if (cbPelabuhan.SelectedItem == null)
            {
                MessageBox.Show("Pilih pelabuhan terlebih dahulu!", "Perhatian",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnCheckPelabuhan.IsEnabled = false;
                txtPelabuhanResult.Text = "🔄 Mengambil data cuaca...";

                dynamic selectedItem = cbPelabuhan.SelectedItem;
                Pelabuhan pelabuhan = selectedItem.Pelabuhan;

                if (pelabuhan.latitude == null || pelabuhan.longitude == null)
                {
                    txtPelabuhanResult.Text = "❌ Pelabuhan tidak memiliki koordinat!";
                    return;
                }

                var weather = await _weatherService.GetMarineWeatherAsync(
                    pelabuhan.latitude.Value,
                    pelabuhan.longitude.Value);

                if (weather != null)
                {
                    txtPelabuhanResult.Inlines.Clear();
                    
                    // Header - Bold
                    AddBoldLine(txtPelabuhanResult, $"=== CUACA PELABUHAN: {pelabuhan.nama_pelabuhan.ToUpper()} ===", 14);
                    AddLine(txtPelabuhanResult, "");
                    
                    // Location info
                    AddSemiBoldText(txtPelabuhanResult, "Lokasi: ");
                    AddLine(txtPelabuhanResult, $"{pelabuhan.kota}, {pelabuhan.provinsi}");
                    
                    AddSemiBoldText(txtPelabuhanResult, "Koordinat: ");
                    AddLine(txtPelabuhanResult, $"{pelabuhan.latitude:F4}, {pelabuhan.longitude:F4}");
                    
                    AddSemiBoldText(txtPelabuhanResult, "Waktu: ");
                    AddLine(txtPelabuhanResult, $"{weather.Time:dd MMMM yyyy HH:mm}");
                    AddLine(txtPelabuhanResult, "");
                    
                    // Weather data
                    AddSemiBoldText(txtPelabuhanResult, "Suhu: ");
                    AddLine(txtPelabuhanResult, $"{weather.Temperature:F1}°C");
                    
                    AddSemiBoldText(txtPelabuhanResult, "Angin: ");
                    AddLine(txtPelabuhanResult, $"{weather.WindSpeed:F1} m/s (Arah: {weather.WindDirection:F0}°)");
                    
                    AddSemiBoldText(txtPelabuhanResult, "Est. Gelombang: ");
                    AddLine(txtPelabuhanResult, $"{weather.EstimatedWaveHeight:F1} meter");
                    
                    AddSemiBoldText(txtPelabuhanResult, "Kelembaban: ");
                    AddLine(txtPelabuhanResult, $"{weather.Humidity:F0}%");
                    
                    AddSemiBoldText(txtPelabuhanResult, "Tekanan: ");
                    AddLine(txtPelabuhanResult, $"{weather.Pressure:F0} hPa");
                    
                    AddSemiBoldText(txtPelabuhanResult, "Visibilitas: ");
                    AddLine(txtPelabuhanResult, $"{weather.Visibility / 1000:F1} km");
                    AddLine(txtPelabuhanResult, "");
                    
                    // Conditions
                    AddSemiBoldText(txtPelabuhanResult, "Kondisi: ");
                    AddLine(txtPelabuhanResult, weather.WeatherDescription);
                    
                    AddSemiBoldText(txtPelabuhanResult, "Status: ");
                    AddLine(txtPelabuhanResult, weather.WeatherCondition);
                    AddLine(txtPelabuhanResult, "");

                    // Safety status
                    string safetyIcon = weather.SafetyLevel switch
                    {
                        "Safe" => "✅",
                        "Moderate" => "⚠️",
                        "Dangerous" => "❌",
                        _ => "❓"
                    };

                    string safetyText = weather.SafetyLevel switch
                    {
                        "Safe" => "AMAN UNTUK BERLAYAR",
                        "Moderate" => "KONDISI MODERATE - HATI-HATI",
                        "Dangerous" => "BERBAHAYA - TIDAK DISARANKAN",
                        _ => "TIDAK DIKETAHUI"
                    };

                    AddBoldLine(txtPelabuhanResult, "=== TINGKAT KEAMANAN ===", 14);
                    AddLine(txtPelabuhanResult, $"{safetyIcon} {safetyText}");
                }
                else
                {
                    txtPelabuhanResult.Text = "Tidak dapat mengambil data cuaca.";
                }
            }
            catch (Exception ex)
            {
                txtPelabuhanResult.Text = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Gagal mengambil data cuaca:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnCheckPelabuhan.IsEnabled = true;
            }
        }

        private async void BtnCheckRoute_Click(object sender, RoutedEventArgs e)
        {
            if (cbPelabuhanAsal.SelectedItem == null || cbPelabuhanTujuan.SelectedItem == null)
            {
                MessageBox.Show("Pilih pelabuhan asal dan tujuan terlebih dahulu!",
                    "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selectedAsal = cbPelabuhanAsal.SelectedItem;
            dynamic selectedTujuan = cbPelabuhanTujuan.SelectedItem;
            Pelabuhan pelabuhanAsal = selectedAsal.Pelabuhan;
            Pelabuhan pelabuhanTujuan = selectedTujuan.Pelabuhan;

            if (pelabuhanAsal.pelabuhan_id == pelabuhanTujuan.pelabuhan_id)
            {
                MessageBox.Show("Pelabuhan asal dan tujuan tidak boleh sama!",
                    "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnCheckRoute.IsEnabled = false;
                txtRouteResult.Text = "Menganalisis rute dengan 5 waypoints...\n\nMohon tunggu...";

                var result = await _weatherService.CheckRouteWithWaypointsAsync(
                    pelabuhanAsal.latitude!.Value,
                    pelabuhanAsal.longitude!.Value,
                    pelabuhanAsal.nama_pelabuhan,
                    pelabuhanTujuan.latitude!.Value,
                    pelabuhanTujuan.longitude!.Value,
                    pelabuhanTujuan.nama_pelabuhan);

                if (result.IsSuccess)
                {
                    txtRouteResult.Inlines.Clear();
                    
                    // Header - Bold
                    AddBoldLine(txtRouteResult, "=== CEK KEAMANAN RUTE PELAYARAN ===", 14);
                    AddLine(txtRouteResult, "");
                    
                    AddSemiBoldText(txtRouteResult, "Rute: ");
                    AddLine(txtRouteResult, $"{pelabuhanAsal.nama_pelabuhan} → {pelabuhanTujuan.nama_pelabuhan}");
                    
                    AddSemiBoldText(txtRouteResult, "Waktu Analisis: ");
                    AddLine(txtRouteResult, $"{DateTime.Now:dd MMM yyyy HH:mm}");
                    AddLine(txtRouteResult, "");
                    
                    AddBoldLine(txtRouteResult, "--- ANALISIS 5 WAYPOINTS ---", 13);
                    AddLine(txtRouteResult, "");

                    foreach (var waypoint in result.Waypoints)
                    {
                        string icon = waypoint.Weather.SafetyLevel switch
                        {
                            "Safe" => "✅",
                            "Moderate" => "⚠️",
                            "Dangerous" => "❌",
                            _ => "❓"
                        };

                        AddBoldText(txtRouteResult, $"{icon} {waypoint.PointName}");
                        AddLine(txtRouteResult, "");
                        
                        AddSemiBoldText(txtRouteResult, "   Koordinat: ");
                        AddLine(txtRouteResult, $"{waypoint.Latitude:F4}, {waypoint.Longitude:F4}");
                        
                        AddSemiBoldText(txtRouteResult, "   Suhu: ");
                        AddLine(txtRouteResult, $"{waypoint.Weather.Temperature:F1}°C");
                        
                        AddSemiBoldText(txtRouteResult, "   Angin: ");
                        AddLine(txtRouteResult, $"{waypoint.Weather.WindSpeed:F1} m/s");
                        
                        AddSemiBoldText(txtRouteResult, "   Est. Gelombang: ");
                        AddLine(txtRouteResult, $"{waypoint.Weather.EstimatedWaveHeight:F1}m");
                        
                        AddSemiBoldText(txtRouteResult, "   Status: ");
                        AddLine(txtRouteResult, waypoint.Weather.SafetyLevel);
                        AddLine(txtRouteResult, "");
                    }

                    AddBoldLine(txtRouteResult, "--- RINGKASAN ANALISIS ---", 13);
                    
                    AddSemiBoldText(txtRouteResult, "Max Angin: ");
                    AddLine(txtRouteResult, $"{result.MaxWindSpeed:F1} m/s");
                    
                    AddSemiBoldText(txtRouteResult, "Max Gelombang: ");
                    AddLine(txtRouteResult, $"{result.MaxWaveHeight:F1}m");
                    
                    AddSemiBoldText(txtRouteResult, "Titik Terburuk: ");
                    AddLine(txtRouteResult, result.WorstPointName);
                    AddLine(txtRouteResult, "");
                    
                    AddBoldLine(txtRouteResult, "=== STATUS ===", 14);
                    AddLine(txtRouteResult, $"{result.OverallIcon} {result.OverallStatus}");
                }
                else
                {
                    txtRouteResult.Text = result.Message;
                }
            }
            catch (Exception ex)
            {
                txtRouteResult.Text = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Gagal menganalisis rute:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnCheckRoute.IsEnabled = true;
            }
        }

        private void CbJadwal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbJadwal.SelectedItem is Jadwal jadwal)
            {
                txtJadwalInfo.Text = $"✅ Rute: {jadwal.pelabuhan_asal?.nama_pelabuhan} → {jadwal.pelabuhan_tujuan?.nama_pelabuhan}\n" +
                                    $"Kapal: {jadwal.kapal?.nama_kapal}\n" +
                                    $"Berangkat: {jadwal.waktu_berangkat:dd MMM yyyy HH:mm}";
                btnGetForecast.IsEnabled = true;
            }
            else
            {
                txtJadwalInfo.Text = "";
                btnGetForecast.IsEnabled = false;
            }
        }

        private async void BtnGetForecast_Click(object sender, RoutedEventArgs e)
        {
            if (cbJadwal.SelectedItem is not Jadwal jadwal)
            {
                MessageBox.Show("Pilih jadwal terlebih dahulu!",
                    "Perhatian", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (jadwal.pelabuhan_asal?.latitude == null || jadwal.pelabuhan_tujuan?.latitude == null)
            {
                MessageBox.Show("Koordinat pelabuhan tidak lengkap!",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                btnGetForecast.IsEnabled = false;

                // Get forecast untuk pelabuhan asal dan tujuan
                var forecastAsal = await _weatherService.GetForecastAsync(
                    jadwal.pelabuhan_asal.latitude.Value,
                    jadwal.pelabuhan_asal.longitude.Value);

                var forecastTujuan = await _weatherService.GetForecastAsync(
                    jadwal.pelabuhan_tujuan.latitude.Value,
                    jadwal.pelabuhan_tujuan.longitude.Value);

                if (forecastAsal != null && forecastTujuan != null)
                {
                    txtForecastResult.Inlines.Clear();
                    
                    AddBoldLine(txtForecastResult, "=== PRAKIRAAN CUACA UNTUK JADWAL ===", 14);
                    AddLine(txtForecastResult, "");

                    // Cari forecast terdekat dengan waktu keberangkatan
                    var departureForecast = forecastAsal
                        .OrderBy(f => Math.Abs((f.DateTime - jadwal.waktu_berangkat).TotalMinutes))
                        .FirstOrDefault();

                    // Cari forecast terdekat dengan waktu tiba
                    var arrivalForecast = forecastTujuan
                        .OrderBy(f => Math.Abs((f.DateTime - jadwal.waktu_tiba).TotalMinutes))
                        .FirstOrDefault();

                    AddBoldLine(txtForecastResult, "--- SAAT KEBERANGKATAN ---", 13);
                    AddLine(txtForecastResult, "");
                    
                    AddSemiBoldText(txtForecastResult, "Pelabuhan: ");
                    AddLine(txtForecastResult, jadwal.pelabuhan_asal.nama_pelabuhan);
                    
                    AddSemiBoldText(txtForecastResult, "Waktu: ");
                    AddLine(txtForecastResult, $"{jadwal.waktu_berangkat:dddd, dd MMM yyyy HH:mm} WIB");
                    AddLine(txtForecastResult, "");

                    if (departureForecast != null)
                    {
                        string departIcon = departureForecast.WindSpeed switch
                        {
                            <= 10 => "✅ AMAN",
                            <= 15 => "⚠️ HATI-HATI",
                            _ => "❌ BERBAHAYA"
                        };

                        AddSemiBoldText(txtForecastResult, "Status Cuaca: ");
                        AddLine(txtForecastResult, departIcon);
                        
                        AddSemiBoldText(txtForecastResult, "Suhu: ");
                        AddLine(txtForecastResult, $"{departureForecast.Temperature:F1}°C");
                        
                        AddSemiBoldText(txtForecastResult, "Kecepatan Angin: ");
                        AddLine(txtForecastResult, $"{departureForecast.WindSpeed:F1} m/s");
                        
                        AddSemiBoldText(txtForecastResult, "Estimasi Gelombang: ");
                        AddLine(txtForecastResult, $"{departureForecast.EstimatedWaveHeight:F1}m");
                        
                        AddSemiBoldText(txtForecastResult, "Kelembaban: ");
                        AddLine(txtForecastResult, $"{departureForecast.Humidity:F0}%");
                        
                        AddSemiBoldText(txtForecastResult, "Kemungkinan Hujan: ");
                        AddLine(txtForecastResult, $"{departureForecast.Pop * 100:F0}%");
                        
                        AddSemiBoldText(txtForecastResult, "Awan: ");
                        AddLine(txtForecastResult, $"{departureForecast.Clouds}%");
                        
                        AddSemiBoldText(txtForecastResult, "Kondisi: ");
                        AddLine(txtForecastResult, departureForecast.WeatherDescription);
                    }
                    else
                    {
                        AddLine(txtForecastResult, "Data forecast tidak tersedia untuk waktu keberangkatan");
                    }

                    AddLine(txtForecastResult, "");
                    AddBoldLine(txtForecastResult, "--- SAAT TIBA ---", 13);
                    AddLine(txtForecastResult, "");
                    
                    AddSemiBoldText(txtForecastResult, "Pelabuhan: ");
                    AddLine(txtForecastResult, jadwal.pelabuhan_tujuan.nama_pelabuhan);
                    
                    AddSemiBoldText(txtForecastResult, "Waktu: ");
                    AddLine(txtForecastResult, $"{jadwal.waktu_tiba:dddd, dd MMM yyyy HH:mm} WIB");
                    AddLine(txtForecastResult, "");

                    if (arrivalForecast != null)
                    {
                        string arriveIcon = arrivalForecast.WindSpeed switch
                        {
                            <= 10 => "✅ AMAN",
                            <= 15 => "⚠️ HATI-HATI",
                            _ => "❌ BERBAHAYA"
                        };

                        AddSemiBoldText(txtForecastResult, "Status Cuaca: ");
                        AddLine(txtForecastResult, arriveIcon);
                        
                        AddSemiBoldText(txtForecastResult, "Suhu: ");
                        AddLine(txtForecastResult, $"{arrivalForecast.Temperature:F1}°C");
                        
                        AddSemiBoldText(txtForecastResult, "Kecepatan Angin: ");
                        AddLine(txtForecastResult, $"{arrivalForecast.WindSpeed:F1} m/s");
                        
                        AddSemiBoldText(txtForecastResult, "Estimasi Gelombang: ");
                        AddLine(txtForecastResult, $"{arrivalForecast.EstimatedWaveHeight:F1}m");
                        
                        AddSemiBoldText(txtForecastResult, "Kelembaban: ");
                        AddLine(txtForecastResult, $"{arrivalForecast.Humidity:F0}%");
                        
                        AddSemiBoldText(txtForecastResult, "Kemungkinan Hujan: ");
                        AddLine(txtForecastResult, $"{arrivalForecast.Pop * 100:F0}%");
                        
                        AddSemiBoldText(txtForecastResult, "Awan: ");
                        AddLine(txtForecastResult, $"{arrivalForecast.Clouds}%");
                        
                        AddSemiBoldText(txtForecastResult, "Kondisi: ");
                        AddLine(txtForecastResult, arrivalForecast.WeatherDescription);
                    }
                    else
                    {
                        AddLine(txtForecastResult, "Data forecast tidak tersedia untuk waktu tiba");
                    }

                    AddLine(txtForecastResult, "");
                    AddBoldLine(txtForecastResult, "--- INFORMASI ---", 13);
                    AddLine(txtForecastResult, "");
                    AddLine(txtForecastResult, "• Data forecast dari OpenWeather API (interval 3 jam)");
                    AddLine(txtForecastResult, "• Forecast dipilih yang paling dekat dengan waktu keberangkatan/tiba");
                    AddLine(txtForecastResult, $"• Durasi perjalanan: {(jadwal.waktu_tiba - jadwal.waktu_berangkat).TotalHours:F1} jam");
                    AddLine(txtForecastResult, "");
                    
                    AddBoldText(txtForecastResult, "⚠️  Kriteria Keamanan:");
                    AddLine(txtForecastResult, "");
                    AddLine(txtForecastResult, "   ✅ Aman: Angin ≤ 10 m/s, Gelombang ≤ 3m");
                    AddLine(txtForecastResult, "   ⚠️  Hati-hati: Angin 10-15 m/s, Gelombang 3-4.5m");
                    AddLine(txtForecastResult, "   ❌ Berbahaya: Angin > 15 m/s, Gelombang > 4.5m");
                }
                else
                {
                    MessageBox.Show("❌ Tidak dapat mengambil data forecast.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal mengambil forecast:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnGetForecast.IsEnabled = true;
            }
        }

        // Helper methods untuk formatting text
        private void AddBoldLine(TextBlock textBlock, string text, double fontSize = 13)
        {
            textBlock.Inlines.Add(new Run(text + "\n") 
            { 
                FontWeight = FontWeights.Bold,
                FontSize = fontSize 
            });
        }

        private void AddBoldText(TextBlock textBlock, string text, double fontSize = 13)
        {
            textBlock.Inlines.Add(new Run(text) 
            { 
                FontWeight = FontWeights.Bold,
                FontSize = fontSize 
            });
        }

        private void AddSemiBoldText(TextBlock textBlock, string text, double fontSize = 13)
        {
            textBlock.Inlines.Add(new Run(text) 
            { 
                FontWeight = FontWeights.SemiBold,
                FontSize = fontSize 
            });
        }

        private void AddLine(TextBlock textBlock, string text, double fontSize = 13)
        {
            textBlock.Inlines.Add(new Run(text + "\n") 
            { 
                FontSize = fontSize 
            });
        }
    }
}
