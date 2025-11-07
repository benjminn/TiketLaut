using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TiketLaut.Models;
using TiketLaut.Services;
using TiketLaut.Views.Admin;

namespace TiketLaut.Views
{
    // Class untuk DataGrid binding pada halaman notifikasi
    public class NotifikasiJadwalItem : INotifyPropertyChanged
    {
        public int JadwalId { get; set; }
        public string StatusIcon { get; set; }
        public string Rute { get; set; }
        public string NamaKapal { get; set; }
        public string WaktuBerangkat { get; set; }
        
        private string _jumlahPenumpang;
        public string JumlahPenumpang
        {
            get => _jumlahPenumpang;
            set
            {
                _jumlahPenumpang = value;
                OnPropertyChanged(nameof(JumlahPenumpang));
            }
        }
        
        public Jadwal Jadwal { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class AdminNotifikasiPage : UserControl
    {
        private readonly NotifikasiService _notifikasiService;
        private readonly PenggunaService _penggunaService;
        private readonly JadwalService _jadwalService;
        private readonly TiketService _tiketService;
        private readonly PelabuhanService _pelabuhanService;
        
        private List<Notifikasi> _allNotifikasi = new();
        private List<Notifikasi> _filteredNotifikasi = new();
        private List<Jadwal> _allJadwals = new();
        private List<Jadwal> _filteredJadwals = new();

        public AdminNotifikasiPage()
        {
            InitializeComponent();
            _notifikasiService = new NotifikasiService();
            _penggunaService = new PenggunaService();
            _jadwalService = new JadwalService();
            _tiketService = new TiketService();
            _pelabuhanService = new PelabuhanService();
            
            Loaded += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadFilterOptionsAsync();
                await LoadUsersAsync();
                await LoadJadwalAsync();
                SetupPreviewBinding();
                
                // Load tab data only when tab is active to avoid DbContext conflicts
                // LoadOtomatisNotifikasi and LoadSemuaNotifikasi will be called when tabs are clicked
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InitializeAsync] Error: {ex.Message}");
                MessageBox.Show($"Error saat inisialisasi: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Tab Navigation

        private void TabKirim_Click(object sender, RoutedEventArgs e)
        {
            ActivateTab(tabKirimContent, btnTabKirim);
        }

        private void TabOtomatis_Click(object sender, RoutedEventArgs e)
        {
            ActivateTab(tabOtomatisContent, btnTabOtomatis);
            _ = LoadOtomatisNotifikasi();
        }

        private void TabSemua_Click(object sender, RoutedEventArgs e)
        {
            ActivateTab(tabSemuaContent, btnTabSemua);
            _ = LoadSemuaNotifikasi();
        }

        private void ActivateTab(UIElement contentToShow, Button activeButton)
        {
            // Hide all tabs
            tabKirimContent.Visibility = Visibility.Collapsed;
            tabOtomatisContent.Visibility = Visibility.Collapsed;
            tabSemuaContent.Visibility = Visibility.Collapsed;

            // Reset all button styles
            btnTabKirim.Style = (Style)FindResource("TabButtonStyle");
            btnTabOtomatis.Style = (Style)FindResource("TabButtonStyle");
            btnTabSemua.Style = (Style)FindResource("TabButtonStyle");

            // Show selected tab and activate button
            contentToShow.Visibility = Visibility.Visible;
            activeButton.Style = (Style)FindResource("ActiveTabButtonStyle");
        }

        #endregion

        #region Tab 1: Kirim Notifikasi

        private async Task LoadFilterOptionsAsync()
        {
            try
            {
                // Null checks untuk controls
                if (cmbFilterPelabuhanAsal == null || cmbFilterPelabuhanTujuan == null)
                    return;

                // Load Pelabuhan untuk filter
                var pelabuhans = await _pelabuhanService.GetAllPelabuhanAsync();
                foreach (var pelabuhan in pelabuhans.OrderBy(p => p.nama_pelabuhan))
                {
                    cmbFilterPelabuhanAsal.Items.Add(new ComboBoxItem { Content = pelabuhan.nama_pelabuhan, Tag = pelabuhan.pelabuhan_id });
                    cmbFilterPelabuhanTujuan.Items.Add(new ComboBoxItem { Content = pelabuhan.nama_pelabuhan, Tag = pelabuhan.pelabuhan_id });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading filter options: {ex.Message}");
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                // Null check untuk control
                if (cmbUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("[LoadUsersAsync] cmbUser is null");
                    return;
                }

                var users = await _penggunaService.GetAllAsync();
                cmbUser.Items.Clear();
                
                foreach (var user in users)
                {
                    cmbUser.Items.Add(new
                    {
                        UserId = user.pengguna_id,
                        DisplayText = $"{user.nama} ({user.email})"
                    });
                }

                if (cmbUser.Items.Count > 0)
                    cmbUser.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadUsersAsync] Error: {ex.Message}");
                MessageBox.Show($"Error memuat data pengguna: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadJadwalAsync()
        {
            try
            {
                _allJadwals = await _jadwalService.GetAllJadwalAsync();
                
                System.Diagnostics.Debug.WriteLine($"[AdminNotifikasiPage] Total jadwal from service: {_allJadwals.Count}");
                
                // Apply filter
                ApplyJadwalFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminNotifikasiPage] Error loading jadwal: {ex.Message}");
                MessageBox.Show($"Error memuat data jadwal: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyJadwalFilter()
        {
            _filteredJadwals = _allJadwals.ToList();

            // Filter by Pelabuhan Asal
            if (cmbFilterPelabuhanAsal?.SelectedIndex > 0)
            {
                var selectedItem = cmbFilterPelabuhanAsal.SelectedItem as ComboBoxItem;
                var pelabuhanId = (int)selectedItem.Tag;
                _filteredJadwals = _filteredJadwals.Where(j => j.pelabuhan_asal_id == pelabuhanId).ToList();
            }

            // Filter by Pelabuhan Tujuan
            if (cmbFilterPelabuhanTujuan?.SelectedIndex > 0)
            {
                var selectedItem = cmbFilterPelabuhanTujuan.SelectedItem as ComboBoxItem;
                var pelabuhanId = (int)selectedItem.Tag;
                _filteredJadwals = _filteredJadwals.Where(j => j.pelabuhan_tujuan_id == pelabuhanId).ToList();
            }

            // Filter by Tanggal
            if (dpFilterTanggal?.SelectedDate.HasValue == true)
            {
                var selectedDate = dpFilterTanggal.SelectedDate.Value.Date;
                _filteredJadwals = _filteredJadwals.Where(j => j.waktu_berangkat.Date == selectedDate).ToList();
            }

            // Filter by Jam
            if (cmbFilterJam?.SelectedIndex > 0)
            {
                var jamIndex = cmbFilterJam.SelectedIndex;
                _filteredJadwals = jamIndex switch
                {
                    1 => _filteredJadwals.Where(j => j.waktu_berangkat.Hour >= 0 && j.waktu_berangkat.Hour < 6).ToList(),   // 00:00 - 06:00
                    2 => _filteredJadwals.Where(j => j.waktu_berangkat.Hour >= 6 && j.waktu_berangkat.Hour < 12).ToList(),  // 06:00 - 12:00
                    3 => _filteredJadwals.Where(j => j.waktu_berangkat.Hour >= 12 && j.waktu_berangkat.Hour < 18).ToList(), // 12:00 - 18:00
                    4 => _filteredJadwals.Where(j => j.waktu_berangkat.Hour >= 18 && j.waktu_berangkat.Hour < 24).ToList(), // 18:00 - 24:00
                    _ => _filteredJadwals
                };
            }

            // Update DataGrid
            if (dgJadwal != null)
            {
                var jadwalList = _filteredJadwals
                    .OrderByDescending(j => j.waktu_berangkat)
                    .Select(j => new NotifikasiJadwalItem
                    {
                        JadwalId = j.jadwal_id,
                        StatusIcon = j.waktu_berangkat >= DateTime.Now ? "🟢" : "🔴",
                        Rute = $"{j.pelabuhan_asal?.nama_pelabuhan ?? "?"} → {j.pelabuhan_tujuan?.nama_pelabuhan ?? "?"}",
                        NamaKapal = j.kapal?.nama_kapal ?? "N/A",
                        WaktuBerangkat = j.waktu_berangkat.ToString("dd MMM yyyy HH:mm"),
                        JumlahPenumpang = "Memuat...",
                        Jadwal = j
                    })
                    .ToList();

                dgJadwal.ItemsSource = jadwalList;

                // Load penumpang count asynchronously
                _ = UpdateJumlahPenumpangAsync(jadwalList);

                System.Diagnostics.Debug.WriteLine($"[AdminNotifikasiPage] Added {jadwalList.Count} items to dgJadwal after filter");

                if (jadwalList.Count == 0 && _allJadwals.Count == 0)
                {
                    MessageBox.Show("Tidak ada jadwal yang tersedia di database.\n\nSilakan tambahkan jadwal terlebih dahulu.",
                        "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async Task UpdateJumlahPenumpangAsync(List<NotifikasiJadwalItem> jadwalList)
        {
            try
            {
                var allTikets = await _tiketService.GetAllTiketsAsync();
                
                foreach (var item in jadwalList)
                {
                    var count = allTikets.Count(t => t.jadwal_id == item.JadwalId);
                    item.JumlahPenumpang = $"{count} orang";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating jumlah penumpang: {ex.Message}");
                foreach (var item in jadwalList)
                {
                    item.JumlahPenumpang = "N/A";
                }
            }
        }

        private void FilterJadwal_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_allJadwals != null && _allJadwals.Count > 0)
            {
                ApplyJadwalFilter();
            }
        }

        private void DgJadwal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedJadwalsDisplay();
        }
        
        private void DgJadwal_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Custom multi-select: klik untuk toggle tanpa perlu Ctrl
            var row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row != null && dgJadwal != null)
            {
                var item = row.Item as NotifikasiJadwalItem;
                if (item != null)
                {
                    e.Handled = true; // Prevent default selection behavior
                    
                    if (dgJadwal.SelectedItems.Contains(item))
                    {
                        // Jika sudah dipilih, deselect
                        dgJadwal.SelectedItems.Remove(item);
                    }
                    else
                    {
                        // Jika belum dipilih, tambahkan ke selection
                        dgJadwal.SelectedItems.Add(item);
                    }
                    
                    UpdateSelectedJadwalsDisplay();
                }
            }
        }
        
        private void BtnRemoveJadwal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int jadwalId)
            {
                // Find and deselect the item in DataGrid
                var itemToRemove = dgJadwal.Items.Cast<NotifikasiJadwalItem>()
                    .FirstOrDefault(item => item.JadwalId == jadwalId);
                
                if (itemToRemove != null)
                {
                    dgJadwal.SelectedItems.Remove(itemToRemove);
                    UpdateSelectedJadwalsDisplay();
                }
            }
        }

        private void CmbPenerima_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Null check - event dipanggil saat XAML loading sebelum controls initialized
            if (pnlSelectUser == null || pnlSelectJadwal == null) return;
            
            // Gunakan sender untuk ambil ComboBox yang trigger event
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;
            
            if (comboBox.SelectedIndex == 1) // Berdasarkan Jadwal
            {
                pnlSelectJadwal.Visibility = Visibility.Visible;
                pnlSelectUser.Visibility = Visibility.Collapsed;
            }
            else if (comboBox.SelectedIndex == 2) // Pengguna Tertentu
            {
                pnlSelectUser.Visibility = Visibility.Visible;
                pnlSelectJadwal.Visibility = Visibility.Collapsed;
            }
            else // Semua Pengguna
            {
                pnlSelectUser.Visibility = Visibility.Collapsed;
                pnlSelectJadwal.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateSelectedJadwalsDisplay()
        {
            if (dgJadwal == null || pnlSelectedJadwals == null || txtSelectedCount == null || icSelectedJadwals == null)
                return;

            var selectedItems = dgJadwal.SelectedItems.Cast<NotifikasiJadwalItem>().ToList();
            
            if (selectedItems.Count == 0)
            {
                pnlSelectedJadwals.Visibility = Visibility.Collapsed;
                txtSelectedCount.Text = "0 jadwal dipilih";
                icSelectedJadwals.ItemsSource = null;
            }
            else
            {
                pnlSelectedJadwals.Visibility = Visibility.Visible;
                txtSelectedCount.Text = $"{selectedItems.Count} jadwal dipilih";
                icSelectedJadwals.ItemsSource = selectedItems;
            }
        }

        private void SetupPreviewBinding()
        {
            txtJudul.TextChanged += (s, e) => UpdatePreview();
            txtPesan.TextChanged += (s, e) => UpdatePreview();
            cmbJenisNotifikasi.SelectionChanged += (s, e) => UpdatePreview();
            
            // DataGrid selection will be handled by DgJadwal_SelectionChanged
        }

        private void UpdatePreview()
        {
            if (txtPreviewJudul == null || txtPreviewPesan == null || imgPreviewIcon == null) return;
            var jenis = (cmbJenisNotifikasi.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "umum";
            var iconPath = GetIconPathForJenis(jenis);

            imgPreviewIcon.Source = new BitmapImage(new Uri($"pack://application:,,,/Views/Assets/Icons/{iconPath}"));

            txtPreviewJudul.Text = string.IsNullOrWhiteSpace(txtJudul.Text) 
                ? "(Judul akan muncul di sini)" 
                : txtJudul.Text;

            txtPreviewPesan.Text = string.IsNullOrWhiteSpace(txtPesan.Text) 
                ? "(Isi pesan akan muncul di sini)" 
                : txtPesan.Text;
        }

        private string GetIconPathForJenis(string jenis)
        {
            return jenis.ToLower() switch
            {
                "pembayaran" => "iconPaymentNotif.png",
                "pengingat" => "iconTimerNotif.png",
                "pemberitahuan" => "iconDangerNotif.png",
                "pembatalan" => "iconGagalNotif.png",
                "umum" => "iconTaskNotif.png",
                _ => "iconNotifikasi.png"
            };
        }

        private async void BtnKirim_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validasi input
                if (string.IsNullOrWhiteSpace(txtJudul.Text))
                {
                    MessageBox.Show("Judul notifikasi tidak boleh kosong!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPesan.Text))
                {
                    MessageBox.Show("Isi pesan tidak boleh kosong!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var jenis = (cmbJenisNotifikasi.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "umum";
                var judul = txtJudul.Text.Trim();
                var pesan = txtPesan.Text.Trim();
                
                var adminId = SessionManager.CurrentAdmin?.admin_id;

                if (cmbPenerima.SelectedIndex == 0) // Semua Pengguna
                {
                    var result = MessageBox.Show(
                        $"Kirim notifikasi ke SEMUA pengguna?\n\nJudul: {judul}\nJenis: {jenis}", 
                        "Konfirmasi", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    var users = await _penggunaService.GetAllAsync();
                    int successCount = 0;

                    foreach (var user in users)
                    {
                        try
                        {
                            await _notifikasiService.CreateNotifikasiAsync(
                                user.pengguna_id,
                                jenis,
                                judul,
                                pesan,
                                olehSystem: false,
                                adminId: adminId
                            );
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error sending to user {user.pengguna_id}: {ex.Message}");
                        }
                    }

                    MessageBox.Show($"Berhasil mengirim notifikasi ke {successCount} pengguna!", "Sukses", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (cmbPenerima.SelectedIndex == 1) // Berdasarkan Jadwal
                {
                    if (dgJadwal.SelectedItems.Count == 0)
                    {
                        MessageBox.Show("Pilih minimal satu jadwal dari tabel terlebih dahulu!", "Validasi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var selectedJadwals = dgJadwal.SelectedItems.Cast<NotifikasiJadwalItem>().ToList();
                    var jadwalCount = selectedJadwals.Count;
                    var jadwalList = string.Join("\n", selectedJadwals.Select(j => $"• {j.Rute} ({j.WaktuBerangkat})"));

                    var result = MessageBox.Show(
                        $"Kirim notifikasi ke pengguna dengan tiket pada {jadwalCount} jadwal berikut?\n\n{jadwalList}\n\nJudul: {judul}\nJenis: {jenis}", 
                        "Konfirmasi", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    // Get all tikets untuk semua jadwal terpilih
                    var allTikets = await _tiketService.GetAllTiketsAsync();
                    var userIdsSet = new HashSet<int>();

                    foreach (var selectedJadwal in selectedJadwals)
                    {
                        var tiketsForJadwal = allTikets.Where(t => t.jadwal_id == selectedJadwal.JadwalId);
                        foreach (var tiket in tiketsForJadwal)
                        {
                            userIdsSet.Add(tiket.pengguna_id);
                        }
                    }

                    if (userIdsSet.Count == 0)
                    {
                        MessageBox.Show("Tidak ada pengguna dengan tiket untuk jadwal yang dipilih.", 
                            "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    int successCount = 0;
                    foreach (var userId in userIdsSet)
                    {
                        try
                        {
                            await _notifikasiService.CreateNotifikasiAsync(
                                userId,
                                jenis,
                                judul,
                                pesan,
                                olehSystem: false,
                                adminId: adminId
                            );
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error sending to user {userId}: {ex.Message}");
                        }
                    }

                    MessageBox.Show($"Berhasil mengirim notifikasi ke {successCount} pengguna dari {jadwalCount} jadwal!", "Sukses", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else // Pengguna Tertentu
                {
                    if (cmbUser.SelectedItem == null)
                    {
                        MessageBox.Show("Pilih pengguna terlebih dahulu!", "Validasi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    dynamic selectedUser = cmbUser.SelectedItem;
                    int userId = selectedUser.UserId;

                    await _notifikasiService.CreateNotifikasiAsync(
                        userId,
                        jenis,
                        judul,
                        pesan,
                        olehSystem: false,
                        adminId: adminId
                    );

                    MessageBox.Show("Notifikasi berhasil dikirim!", "Sukses", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ClearForm();
                await LoadSemuaNotifikasi(); // Refresh data
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error mengirim notifikasi: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            txtJudul.Clear();
            txtPesan.Clear();
            cmbPenerima.SelectedIndex = 0;
            cmbJenisNotifikasi.SelectedIndex = 1; // Pemberitahuan
            UpdatePreview();
        }

        #endregion

        #region Tab 2: Notifikasi Otomatis

        private async Task LoadOtomatisNotifikasi()
        {
            try
            {
                // Null check untuk control
                if (dgOtomatis == null)
                {
                    System.Diagnostics.Debug.WriteLine("[LoadOtomatisNotifikasi] dgOtomatis is null");
                    return;
                }

                var allNotif = await _notifikasiService.GetAllNotifikasiAsync();
                var otomatis = allNotif.Where(n => n.oleh_system && n.jadwal_id.HasValue).ToList();

                // Group by jadwal_id and waktu_kirim (date only) untuk mengelompokkan batch pengiriman
                var grouped = otomatis
                    .GroupBy(n => new
                    {
                        JadwalId = n.jadwal_id.Value,
                        WaktuKirimDate = n.waktu_kirim.Date,
                        Kategori = n.judul_notifikasi.Contains("24 jam") ? "Pengingat H-1 (24 Jam)" :
                                   n.judul_notifikasi.Contains("2 jam") ? "Pengingat H-0 (2 Jam)" :
                                   n.jenis_notifikasi == "pembayaran" ? "Konfirmasi Pembayaran" :
                                   "Notifikasi Sistem"
                    })
                    .Select(g =>
                    {
                        var firstNotif = g.First();
                        var jadwal = firstNotif.Jadwal;

                        return new
                        {
                            Kategori = g.Key.Kategori,
                            RuteJadwal = jadwal != null
                                ? $"{jadwal.pelabuhan_asal?.nama_pelabuhan ?? "?"} → {jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "?"}"
                                : "N/A",
                            NamaKapal = jadwal?.kapal?.nama_kapal ?? "N/A",
                            WaktuKeberangkatan = jadwal?.waktu_berangkat ?? DateTime.MinValue,
                            WaktuKirim = g.Min(n => n.waktu_kirim),
                            JumlahPenerima = g.Count()
                        };
                    })
                    .OrderByDescending(x => x.WaktuKirim)
                    .ToList();

                ApplyOtomatisFilter(grouped);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadOtomatisNotifikasi] Error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error memuat notifikasi otomatis: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyOtomatisFilter(object groupedData)
        {
            // Null check untuk control
            if (dgOtomatis == null)
            {
                System.Diagnostics.Debug.WriteLine("[ApplyOtomatisFilter] dgOtomatis is null");
                return;
            }

            var filtered = (groupedData as IEnumerable<dynamic>)?.AsEnumerable();
            
            if (filtered == null)
            {
                dgOtomatis.ItemsSource = null;
                return;
            }

            // Filter by Kategori
            if (cmbFilterKategori?.SelectedIndex > 0)
            {
                var selectedKategori = (cmbFilterKategori.SelectedItem as ComboBoxItem)?.Content.ToString();
                filtered = filtered.Where(x => x.Kategori == selectedKategori);
            }

            // Filter by Periode
            if (cmbFilterPeriode?.SelectedIndex > 0)
            {
                var now = DateTime.Now;
                var selectedPeriode = cmbFilterPeriode.SelectedIndex;

                filtered = selectedPeriode switch
                {
                    1 => filtered.Where(x => ((DateTime)x.WaktuKirim).Date == now.Date), // Hari Ini
                    2 => filtered.Where(x => ((DateTime)x.WaktuKirim) >= now.AddDays(-7)), // 7 Hari Terakhir
                    3 => filtered.Where(x => ((DateTime)x.WaktuKirim) >= now.AddDays(-30)), // 30 Hari Terakhir
                    _ => filtered
                };
            }

            dgOtomatis.ItemsSource = filtered.ToList();
        }

        private void FilterOtomatis_Changed(object sender, SelectionChangedEventArgs e)
        {
            _ = LoadOtomatisNotifikasi();
        }

        private async void BtnRefreshOtomatis_Click(object sender, RoutedEventArgs e)
        {
            await LoadOtomatisNotifikasi();
        }

        #endregion

        #region Tab 3: Semua Notifikasi

        private async Task LoadSemuaNotifikasi()
        {
            try
            {
                _allNotifikasi = await _notifikasiService.GetAllNotifikasiAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error memuat semua notifikasi: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            // Null check - method bisa dipanggil saat initialization
            if (cmbFilterJenis == null || cmbFilterSumber == null || dgSemua == null || txtTotalNotif == null)
                return;
                
            _filteredNotifikasi = _allNotifikasi.ToList();

            // Filter by Jenis
            if (cmbFilterJenis.SelectedIndex > 0)
            {
                var jenisText = (cmbFilterJenis.SelectedItem as ComboBoxItem)?.Content?.ToString();
                _filteredNotifikasi = _filteredNotifikasi
                    .Where(n => n.jenis_notifikasi.Equals(jenisText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filter by Sumber
            if (cmbFilterSumber.SelectedIndex == 1) // Otomatis
            {
                _filteredNotifikasi = _filteredNotifikasi.Where(n => n.oleh_system).ToList();
            }
            else if (cmbFilterSumber.SelectedIndex == 2) // Manual
            {
                _filteredNotifikasi = _filteredNotifikasi.Where(n => !n.oleh_system).ToList();
            }

            dgSemua.ItemsSource = _filteredNotifikasi.Select(n => new
            {
                n.notifikasi_id,
                n.Pengguna,
                n.jenis_notifikasi,
                n.judul_notifikasi,
                sumber_notifikasi = n.oleh_system ? "Sistem" : "Admin",
                n.waktu_kirim,
                dibaca = n.status_baca ? "Dibaca" : "Belum"
            }).ToList();

            txtTotalNotif.Text = $"Total: {_filteredNotifikasi.Count} notifikasi";
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            cmbFilterJenis.SelectedIndex = 0;
            cmbFilterSumber.SelectedIndex = 0;
        }

        private async void BtnRefreshSemua_Click(object sender, RoutedEventArgs e)
        {
            await LoadSemuaNotifikasi();
        }

        private async void BtnHapusLama_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Hapus semua notifikasi yang lebih dari 30 hari?\n\nAksi ini tidak dapat dibatalkan!", 
                    "Konfirmasi", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                var deletedCount = await _notifikasiService.DeleteOldNotificationsAsync(30);
                
                MessageBox.Show($"Berhasil menghapus {deletedCount} notifikasi lama.", "Sukses", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadSemuaNotifikasi();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error menghapus notifikasi lama: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDetailNotif_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag == null) return;

                int notifId = Convert.ToInt32(button.Tag);
                var notifikasi = _notifikasiService.GetNotifikasiById(notifId);

                if (notifikasi == null)
                {
                    MessageBox.Show("Notifikasi tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new NotifikasiDetailDialog(notifikasi, _notifikasiService.GetContext());
                if (dialog.ShowDialog() == true)
                {
                    // Refresh data jika ada perubahan
                    _ = LoadSemuaNotifikasi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error membuka detail notifikasi: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
