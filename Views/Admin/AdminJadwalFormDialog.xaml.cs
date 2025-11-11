using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminJadwalFormDialog : Window
    {
        private readonly JadwalService _jadwalService;
        private readonly PelabuhanService _pelabuhanService;
        private readonly KapalService _kapalService;
        private readonly GrupKendaraanService _grupKendaraanService;
        private Jadwal? _existingJadwal;
        private bool _isEditMode;
        private ObservableCollection<BulkTimeRow> _bulkTimeRows;
        private ObservableCollection<DateTime> _selectedDates;
        private ObservableCollection<DetailKendaraanInputRow> _detailKendaraanRows;
        private bool _isDragging = false;
        private DateTime? _dragStartDate = null;
        private bool _isDragSelecting = true; // true = select, false = deselect

        public AdminJadwalFormDialog(Jadwal? jadwal = null)
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            _pelabuhanService = new PelabuhanService();
            _kapalService = new KapalService();
            _grupKendaraanService = new GrupKendaraanService();
            _existingJadwal = jadwal;
            _isEditMode = jadwal != null;
            _bulkTimeRows = new ObservableCollection<BulkTimeRow>();
            dgBulkWaktu.ItemsSource = _bulkTimeRows;
            _selectedDates = new ObservableCollection<DateTime>();
            icSelectedDates.ItemsSource = _selectedDates;
            _detailKendaraanRows = new ObservableCollection<DetailKendaraanInputRow>();
            dgDetailKendaraan.ItemsSource = _detailKendaraanRows;

            // Load initial data dan jadwal data (jika edit mode)
            LoadInitialDataAndJadwal();
        }

        private async void LoadInitialDataAndJadwal()
        {
            try
            {
                // Load pelabuhan
                var pelabuhans = await _pelabuhanService.GetAllPelabuhanAsync();
                cbPelabuhanAsal.ItemsSource = pelabuhans;
                cbPelabuhanTujuan.ItemsSource = pelabuhans;

                // Load kapal
                var kapals = await _kapalService.GetAllKapalAsync();
                cbKapal.ItemsSource = kapals;

                // Load existing grup kendaraan
                await LoadGrupKendaraanAsync();

                // Load Detail Kendaraan DataGrid (semua 13 golongan dengan input harga)
                LoadDetailKendaraanGrid();

                // Jika edit mode, load data jadwal SETELAH semua data master ter-load
                if (_isEditMode && _existingJadwal != null)
                {
                    txtTitle.Text = "Edit Jadwal";
                    btnSave.Content = "Update";
                    btnBulkSave.Visibility = Visibility.Collapsed;
                    dgBulkWaktu.IsEnabled = false;
                    btnAddRow.IsEnabled = false;
                    btnClearRows.IsEnabled = false;
                    calendarMultiSelect.IsEnabled = false;
                    btnClearDates.IsEnabled = false;
                    
                    // Disable grup kendaraan section di edit mode (grup tidak bisa diubah)
                    rbGunakanGrupLama.IsEnabled = false;
                    rbBuatGrupBaru.IsEnabled = false;
                    cbGrupKendaraan.IsEnabled = false;
                    txtNamaGrup.IsEnabled = false;
                    dgDetailKendaraan.IsReadOnly = true;
                    dgDetailKendaraan.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(245, 245, 245));
                    
                    // Load jadwal data setelah grup kendaraan ter-load
                    LoadJadwalData(_existingJadwal);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void LoadJadwalData(Jadwal jadwal)
        {
            cbPelabuhanAsal.SelectedValue = jadwal.pelabuhan_asal_id;
            cbPelabuhanTujuan.SelectedValue = jadwal.pelabuhan_tujuan_id;
            cbKapal.SelectedValue = jadwal.kapal_id;
            
            // Load tanggal dan waktu berangkat
            dpTanggalBerangkat.SelectedDate = jadwal.waktu_berangkat.Date;
            txtJamBerangkat.Text = jadwal.waktu_berangkat.ToString("HH");
            txtMenitBerangkat.Text = jadwal.waktu_berangkat.ToString("mm");
            
            // Calculate durasi
            var durasi = jadwal.waktu_tiba - jadwal.waktu_berangkat;
            txtDurasiJam.Text = ((int)durasi.TotalHours).ToString();
            txtDurasiMenit.Text = durasi.Minutes.ToString();
            
            // Set kelas layanan
            foreach (var item in cbKelasLayanan.Items.Cast<System.Windows.Controls.ComboBoxItem>())
            {
                if (item.Content.ToString() == jadwal.kelas_layanan)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            // Set status
            foreach (var item in cbStatus.Items.Cast<System.Windows.Controls.ComboBoxItem>())
            {
                if (item.Content.ToString() == jadwal.status)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            // Load grup kendaraan yang digunakan oleh jadwal ini
            if (jadwal.grup_kendaraan_id > 0)
            {
                // Set radio button ke "Gunakan Grup Lama"
                rbGunakanGrupLama.IsChecked = true;
                
                // Set selected grup kendaraan
                cbGrupKendaraan.SelectedValue = jadwal.grup_kendaraan_id;
            }
        }

        private async Task LoadGrupKendaraanAsync()
        {
            try
            {
                // Load fresh data setiap kali dipanggil untuk menghindari stale data
                var allGrups = await _grupKendaraanService.GetAllGrupWithUsageAsync();
                cbGrupKendaraan.ItemsSource = null;
                cbGrupKendaraan.ItemsSource = allGrups;
                
                // JANGAN set DisplayMemberPath karena XAML sudah punya ItemTemplate
                // DisplayMemberPath dan ItemTemplate tidak bisa digunakan bersamaan
                // SelectedValuePath sudah di-set di XAML
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading grup kendaraan: {ex.Message}\n\nStack Trace: {ex.StackTrace}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RbGunakanGrupLama_Checked(object sender, RoutedEventArgs e)
        {
            if (cbGrupKendaraan != null && txtNamaGrup != null && dgDetailKendaraan != null)
            {
                cbGrupKendaraan.IsEnabled = true;
                txtNamaGrup.IsEnabled = false;
                txtNamaGrup.Text = "";
                dgDetailKendaraan.IsReadOnly = true;
                dgDetailKendaraan.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(245, 245, 245));
            }
        }

        private void RbBuatGrupBaru_Checked(object sender, RoutedEventArgs e)
        {
            if (cbGrupKendaraan != null && txtNamaGrup != null && dgDetailKendaraan != null)
            {
                cbGrupKendaraan.IsEnabled = false;
                cbGrupKendaraan.SelectedIndex = -1;
                txtNamaGrup.IsEnabled = true;
                dgDetailKendaraan.IsReadOnly = false;
                dgDetailKendaraan.Background = System.Windows.Media.Brushes.White;
                foreach (var row in _detailKendaraanRows)
                {
                    row.Harga = 0;
                }
            }
        }

        private void CbGrupKendaraan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbGrupKendaraan.SelectedItem is GrupKendaraanWithUsage selectedGrup)
            {
                // Load harga dari grup yang dipilih ke DataGrid (read-only)
                foreach (var row in _detailKendaraanRows)
                {
                    var detailKendaraan = selectedGrup.detail_kendaraans
                        .FirstOrDefault(d => d.jenis_kendaraan == (int)row.JenisKendaraanEnum);
                    if (detailKendaraan != null)
                    {
                        row.Harga = detailKendaraan.harga_kendaraan;
                    }
                }
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CalendarMultiSelect_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach double-click handlers to all day buttons
            AttachDoubleClickHandlers();
            
            // Re-attach when month/year changes
            calendarMultiSelect.DisplayDateChanged += (s, ev) =>
            {
                Dispatcher.BeginInvoke(new Action(() => AttachDoubleClickHandlers()), 
                    System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        private void AttachDoubleClickHandlers()
        {
            var dayButtons = FindVisualChildren<System.Windows.Controls.Primitives.CalendarDayButton>(calendarMultiSelect);
            foreach (var button in dayButtons)
            {
                button.PreviewMouseLeftButtonDown -= DayButton_PreviewMouseLeftButtonDown;
                button.MouseEnter -= DayButton_MouseEnter;
                button.PreviewMouseLeftButtonUp -= DayButton_PreviewMouseLeftButtonUp;
                button.PreviewMouseLeftButtonDown += DayButton_PreviewMouseLeftButtonDown;
                button.MouseEnter += DayButton_MouseEnter;
                button.PreviewMouseLeftButtonUp += DayButton_PreviewMouseLeftButtonUp;
            }
        }

        private void DayButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.CalendarDayButton dayButton &&
                dayButton.DataContext is DateTime clickedDate)
            {
                var date = clickedDate.Date;
                
                if (e.ClickCount == 2) // Double-click: toggle
                {
                    if (_selectedDates.Contains(date))
                    {
                        _selectedDates.Remove(date);
                    }
                    else
                    {
                        _selectedDates.Add(date);
                    }
                    
                    // Sort and update
                    var sortedDates = _selectedDates.OrderBy(d => d).ToList();
                    _selectedDates.Clear();
                    foreach (var d in sortedDates)
                    {
                        _selectedDates.Add(d);
                    }
                    
                    UpdateSelectedCountText();
                    UpdateDayButtonStyles();
                    e.Handled = true;
                }
                else // Single click: start potential drag
                {
                    _isDragging = true;
                    _dragStartDate = date;
                    
                    // Tentukan mode drag: select (jika belum terpilih) atau deselect (jika sudah terpilih)
                    _isDragSelecting = !_selectedDates.Contains(date);
                    
                    // Toggle the clicked date
                    if (_isDragSelecting)
                    {
                        if (!_selectedDates.Contains(date))
                        {
                            _selectedDates.Add(date);
                        }
                    }
                    else
                    {
                        if (_selectedDates.Contains(date))
                        {
                            _selectedDates.Remove(date);
                        }
                    }
                    
                    UpdateSelectedCountText();
                    UpdateDayButtonStyles();
                }
            }
        }

        private void DayButton_MouseEnter(object sender, MouseEventArgs e)
        {
            // Trigger saat mouse masuk ke button (lebih reliable untuk drag selection)
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is System.Windows.Controls.Primitives.CalendarDayButton dayButton &&
                    dayButton.DataContext is DateTime currentDate)
                {
                    var date = currentDate.Date;
                    
                    if (_dragStartDate.HasValue)
                    {
                        // Pilih SEMUA tanggal di antara _dragStartDate dan currentDate
                        var startDate = _dragStartDate.Value < date ? _dragStartDate.Value : date;
                        var endDate = _dragStartDate.Value > date ? _dragStartDate.Value : date;
                        var originalDates = _selectedDates.ToList();
                        _selectedDates.Clear();
                        foreach (var origDate in originalDates)
                        {
                            if (origDate < startDate || origDate > endDate)
                            {
                                _selectedDates.Add(origDate);
                            }
                        }
                        if (_isDragSelecting)
                        {
                            // Mode select: tambahkan semua tanggal dalam range
                            for (var d = startDate; d <= endDate; d = d.AddDays(1))
                            {
                                if (!_selectedDates.Contains(d))
                                {
                                    _selectedDates.Add(d);
                                }
                            }
                        }
                        else
                        {
                            // Mode deselect: hapus semua tanggal dalam range
                            for (var d = startDate; d <= endDate; d = d.AddDays(1))
                            {
                                _selectedDates.Remove(d);
                            }
                        }
                        
                        // Sort dates
                        var sortedDates = _selectedDates.OrderBy(d => d).ToList();
                        _selectedDates.Clear();
                        foreach (var d in sortedDates)
                        {
                            _selectedDates.Add(d);
                        }
                        
                        UpdateSelectedCountText();
                        UpdateDayButtonStyles();
                    }
                }
            }
        }

        private void DayButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _dragStartDate = null;
                // Tidak perlu ReleaseMouseCapture() karena tidak pakai CaptureMouse()
            }
        }

        private void UpdateDayButtonStyles()
        {
            var dayButtons = FindVisualChildren<System.Windows.Controls.Primitives.CalendarDayButton>(calendarMultiSelect);
            foreach (var button in dayButtons)
            {
                if (button.DataContext is DateTime date)
                {
                    if (_selectedDates.Contains(date.Date))
                    {
                        // Selected style
                        button.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0, 101, 141)); // #00658D
                        button.Foreground = System.Windows.Media.Brushes.White;
                        button.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        // Default style
                        button.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                        button.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
                        button.ClearValue(System.Windows.Controls.Control.FontWeightProperty);
                    }
                }
            }
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                    yield return typedChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }



        private void BtnClearDates_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDates.Count > 0)
            {
                var result = MessageBox.Show("Hapus semua tanggal yang dipilih?", "Konfirmasi", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _selectedDates.Clear();
                    UpdateSelectedCountText();
                    UpdateDayButtonStyles(); // Update visual
                }
            }
        }

        private void UpdateSelectedCountText()
        {
            if (_selectedDates.Count == 0)
            {
                txtSelectedCount.Text = "Belum ada tanggal dipilih";
            }
            else if (_selectedDates.Count == 1)
            {
                txtSelectedCount.Text = "1 tanggal dipilih";
            }
            else
            {
                txtSelectedCount.Text = $"{_selectedDates.Count} tanggal dipilih";
            }
        }

        private void BtnHapusTanggal_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag is DateTime dateToRemove)
            {
                _selectedDates.Remove(dateToRemove);
                UpdateSelectedCountText();
                UpdateDayButtonStyles(); // Update visual
            }
        }

        private void OnWaktuBerangkatChanged(object sender, EventArgs e)
        {
            CalculateWaktuTiba();
        }

        private void OnDurasiChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateWaktuTiba();
            
            // Recalculate all bulk arrival times when duration changes
            if (_bulkTimeRows != null)
            {
                foreach (var row in _bulkTimeRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.JamBerangkat))
                    {
                        row.CalculateJamTiba(txtDurasiJam, txtDurasiMenit);
                    }
                }
            }
        }

        private void CalculateWaktuTiba()
        {
            try
            {
                if (dpTanggalBerangkat.SelectedDate == null ||
                    string.IsNullOrEmpty(txtJamBerangkat.Text) ||
                    string.IsNullOrEmpty(txtMenitBerangkat.Text) ||
                    string.IsNullOrEmpty(txtDurasiJam.Text) ||
                    string.IsNullOrEmpty(txtDurasiMenit.Text))
                {
                    return;
                }

                var tanggal = dpTanggalBerangkat.SelectedDate.Value;
                var jam = int.Parse(txtJamBerangkat.Text);
                var menit = int.Parse(txtMenitBerangkat.Text);
                var durasiJam = int.Parse(txtDurasiJam.Text);
                var durasiMenit = int.Parse(txtDurasiMenit.Text);

                var waktuBerangkat = new DateTime(tanggal.Year, tanggal.Month, tanggal.Day, jam, menit, 0);
                var waktuTiba = waktuBerangkat.AddHours(durasiJam).AddMinutes(durasiMenit);

                txtWaktuTiba.Text = waktuTiba.ToString("dd/MM/yyyy HH:mm");
            }
            catch
            {
                // Ignore parsing errors during input
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            btnSave.IsEnabled = false;
            btnSave.Content = "Menyimpan...";

            try
            {
                var pelabuhan_asal_id = (int)cbPelabuhanAsal.SelectedValue;
                var pelabuhan_tujuan_id = (int)cbPelabuhanTujuan.SelectedValue;
                var kapal_id = (int)cbKapal.SelectedValue;
                var kelas_layanan = ((System.Windows.Controls.ComboBoxItem)cbKelasLayanan.SelectedItem).Content.ToString()!;
                var status = ((System.Windows.Controls.ComboBoxItem)cbStatus.SelectedItem).Content.ToString()!;
                var jam = int.Parse(txtJamBerangkat.Text);
                var menit = int.Parse(txtMenitBerangkat.Text);
                var durasiJam = int.Parse(txtDurasiJam.Text);
                var durasiMenit = int.Parse(txtDurasiMenit.Text);

                                var pelabuhanAsal = await _pelabuhanService.GetPelabuhanByIdAsync(pelabuhan_asal_id);
                var timezoneOffsetHours = pelabuhanAsal?.TimezoneOffsetHours ?? 7;  // Default WIB

                if (_isEditMode && _existingJadwal != null)
                {
                    var tanggal = _existingJadwal.waktu_berangkat.Date;
                    
                                        // Admin input waktu dalam timezone pelabuhan, convert ke UTC untuk database
                    var waktuLokal = new DateTime(tanggal.Year, tanggal.Month, tanggal.Day, jam, menit, 0);
                    var waktu_berangkat = waktuLokal.AddHours(-timezoneOffsetHours);
                    var waktu_tiba = waktu_berangkat.AddHours(durasiJam).AddMinutes(durasiMenit);
                    
                    _existingJadwal.pelabuhan_asal_id = pelabuhan_asal_id;
                    _existingJadwal.pelabuhan_tujuan_id = pelabuhan_tujuan_id;
                    _existingJadwal.kapal_id = kapal_id;
                    _existingJadwal.waktu_berangkat = waktu_berangkat;
                    _existingJadwal.waktu_tiba = waktu_tiba;
                    _existingJadwal.kelas_layanan = kelas_layanan;
                    _existingJadwal.status = status;

                    var result = await _jadwalService.UpdateJadwalAsync(_existingJadwal);
                    MessageBox.Show(result.message, result.success ? "Success" : "Error",
                        MessageBoxButton.OK, result.success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.success)
                    {
                        DialogResult = true;
                        Close();
                    }
                }
                else
                {
                    if (_selectedDates.Count == 0)
                    {
                        MessageBox.Show("Pilih minimal satu tanggal terlebih dahulu!", "Validasi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnSave.IsEnabled = true;
                        btnSave.Content = "💾 Simpan";
                        return;
                    }

                    int grupKendaraanId;
                    string grupNama;
                    if (rbGunakanGrupLama.IsChecked == true)
                    {
                        // Mode: Gunakan Grup Lama
                        if (cbGrupKendaraan.SelectedValue == null)
                        {
                            MessageBox.Show("Pilih grup kendaraan yang akan digunakan!", "Validasi", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            btnSave.IsEnabled = true;
                            btnSave.Content = "💾 Simpan";
                            return;
                        }

                        grupKendaraanId = (int)cbGrupKendaraan.SelectedValue;
                        grupNama = ((GrupKendaraanWithUsage)cbGrupKendaraan.SelectedItem).nama_grup_kendaraan;
                    }
                    else
                    {
                        // Mode: Buat Grup Baru
                        // Validasi: nama grup harus diisi
                        if (string.IsNullOrWhiteSpace(txtNamaGrup.Text))
                        {
                            MessageBox.Show("Nama grup harga wajib diisi!", "Validasi", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            btnSave.IsEnabled = true;
                            btnSave.Content = "💾 Simpan";
                            return;
                        }

                        // Validasi: semua 13 golongan harus diisi harganya
                        var unfilledRows = _detailKendaraanRows.Where(r => r.Harga <= 0).ToList();
                        if (unfilledRows.Any())
                        {
                            var golonganList = string.Join(", ", unfilledRows.Select(r => r.Golongan));
                            MessageBox.Show($"Semua golongan kendaraan harus diisi harganya!\n\nBelum diisi: {golonganList}", 
                                "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                            btnSave.IsEnabled = true;
                            btnSave.Content = "💾 Simpan";
                            return;
                        }

                        // Build prices dictionary from DataGrid
                        var prices = new Dictionary<JenisKendaraan, decimal>();
                        foreach (var row in _detailKendaraanRows)
                        {
                            prices[row.JenisKendaraanEnum] = row.Harga;
                        }
                        var grupResult = await _grupKendaraanService.CreateGrupWithDetailAsync(
                            txtNamaGrup.Text.Trim(), prices);
                        
                        if (grupResult.grup == null)
                        {
                            MessageBox.Show("Error membuat grup kendaraan", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            btnSave.IsEnabled = true;
                            btnSave.Content = "💾 Simpan";
                            return;
                        }

                        grupKendaraanId = grupResult.grup.grup_kendaraan_id;
                        grupNama = grupResult.grup.nama_grup_kendaraan;
                    }
                    
                    var jadwals = new List<Jadwal>();
                    foreach (var dateToCreate in _selectedDates)
                    {
                                                // Admin input waktu dalam timezone pelabuhan, convert ke UTC untuk database
                        var waktuLokal = new DateTime(
                            dateToCreate.Year, dateToCreate.Month, dateToCreate.Day, 
                            jam, menit, 0);
                        var waktu_berangkat_for_date = waktuLokal.AddHours(-timezoneOffsetHours);
                        var waktu_tiba_for_date = waktu_berangkat_for_date.AddHours(durasiJam).AddMinutes(durasiMenit);
                        
                        jadwals.Add(new Jadwal
                        {
                            pelabuhan_asal_id = pelabuhan_asal_id,
                            pelabuhan_tujuan_id = pelabuhan_tujuan_id,
                            kapal_id = kapal_id,
                            waktu_berangkat = waktu_berangkat_for_date,
                            waktu_tiba = waktu_tiba_for_date,
                            kelas_layanan = kelas_layanan,
                            status = status,
                            grup_kendaraan_id = grupKendaraanId
                        });
                    }

                    // Use bulk create for all jadwals
                    var result = await _jadwalService.BulkCreateJadwalAsync(jadwals);
                    var totalDates = _selectedDates.Count;
                    var totalJadwals = jadwals.Count;
                    MessageBox.Show(
                        $"{result.message}\n\nGrup: \"{grupNama}\"\nTotal: {totalJadwals} jadwal dibuat untuk {totalDates} tanggal\n(Setiap jadwal memiliki 13 golongan kendaraan dalam 1 grup)", 
                        result.success ? "Success" : "Error",
                        MessageBoxButton.OK, result.success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.success)
                    {
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSave.IsEnabled = true;
                btnSave.Content = _isEditMode ? "Update" : "Simpan";
            }
        }

        private async void BtnBulkSave_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (cbPelabuhanAsal.SelectedValue == null || cbPelabuhanTujuan.SelectedValue == null || cbKapal.SelectedValue == null)
            {
                MessageBox.Show("Lengkapi data pelabuhan dan kapal!", "Validasi", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_bulkTimeRows.Count == 0)
            {
                MessageBox.Show("Tambahkan minimal satu baris waktu untuk bulk create!", "Validasi", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedDates.Count == 0)
            {
                MessageBox.Show("Pilih minimal satu tanggal terlebih dahulu!", "Validasi", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDurasiJam.Text) || string.IsNullOrWhiteSpace(txtDurasiMenit.Text))
            {
                MessageBox.Show("Masukkan durasi perjalanan!", "Validasi", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnBulkSave.IsEnabled = false;
            btnBulkSave.Content = "Menyimpan...";

            try
            {
                var pelabuhan_asal_id = (int)cbPelabuhanAsal.SelectedValue;
                var pelabuhan_tujuan_id = (int)cbPelabuhanTujuan.SelectedValue;
                var kapal_id = (int)cbKapal.SelectedValue;
                var kelas_layanan = ((System.Windows.Controls.ComboBoxItem)cbKelasLayanan.SelectedItem).Content.ToString()!;
                var status = ((System.Windows.Controls.ComboBoxItem)cbStatus.SelectedItem).Content.ToString()!;
                
                var durasiJam = int.Parse(txtDurasiJam.Text);
                var durasiMenit = int.Parse(txtDurasiMenit.Text);

                                var pelabuhanAsal = await _pelabuhanService.GetPelabuhanByIdAsync(pelabuhan_asal_id);
                var timezoneOffsetHours = pelabuhanAsal?.TimezoneOffsetHours ?? 7;  // Default WIB

                int grupKendaraanId;
                string grupNama;
                if (rbGunakanGrupLama.IsChecked == true)
                {
                    // Mode: Gunakan Grup Lama
                    if (cbGrupKendaraan.SelectedValue == null)
                    {
                        MessageBox.Show("Pilih grup kendaraan yang akan digunakan!", "Validasi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnBulkSave.IsEnabled = true;
                        btnBulkSave.Content = "💾 Bulk Save";
                        return;
                    }

                    grupKendaraanId = (int)cbGrupKendaraan.SelectedValue;
                    grupNama = ((GrupKendaraanWithUsage)cbGrupKendaraan.SelectedItem).nama_grup_kendaraan;
                }
                else
                {
                    // Mode: Buat Grup Baru
                    // Validasi: nama grup harus diisi
                    if (string.IsNullOrWhiteSpace(txtNamaGrup.Text))
                    {
                        MessageBox.Show("Nama grup harga wajib diisi!", "Validasi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnBulkSave.IsEnabled = true;
                        btnBulkSave.Content = "💾 Bulk Save";
                        return;
                    }

                    // Validasi: semua 13 golongan harus diisi harganya
                    var unfilledRows = _detailKendaraanRows.Where(r => r.Harga <= 0).ToList();
                    if (unfilledRows.Any())
                    {
                        var golonganList = string.Join(", ", unfilledRows.Select(r => r.Golongan));
                        MessageBox.Show($"Semua golongan kendaraan harus diisi harganya!\n\nBelum diisi: {golonganList}", 
                            "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnBulkSave.IsEnabled = true;
                        btnBulkSave.Content = "💾 Bulk Save";
                        return;
                    }

                    // Build prices dictionary from DataGrid
                    var prices = new Dictionary<JenisKendaraan, decimal>();
                    foreach (var row in _detailKendaraanRows)
                    {
                        prices[row.JenisKendaraanEnum] = row.Harga;
                    }
                    var grupResult = await _grupKendaraanService.CreateGrupWithDetailAsync(
                        txtNamaGrup.Text.Trim(), prices);
                    
                    if (grupResult.grup == null)
                    {
                        MessageBox.Show("Error membuat grup kendaraan", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        btnBulkSave.IsEnabled = true;
                        btnBulkSave.Content = "💾 Bulk Save";
                        return;
                    }

                    grupKendaraanId = grupResult.grup.grup_kendaraan_id;
                    grupNama = grupResult.grup.nama_grup_kendaraan;
                }

                // Use selected dates
                var datesToCreate = _selectedDates.ToList();

                var jadwals = new List<Jadwal>();
                var invalidRows = new List<int>();

                // Process DataGrid rows for each selected date
                foreach (var dateToCreate in datesToCreate)
                {
                    foreach (var row in _bulkTimeRows)
                    {
                        if (string.IsNullOrWhiteSpace(row.JamBerangkat))
                        {
                            if (!invalidRows.Contains(row.No))
                                invalidRows.Add(row.No);
                            continue;
                        }

                        var parts = row.JamBerangkat.Split(':');
                        if (parts.Length == 2 && 
                            int.TryParse(parts[0], out int jam) && jam >= 0 && jam <= 23 &&
                            int.TryParse(parts[1], out int menit) && menit >= 0 && menit <= 59)
                        {
                                                        // Admin input waktu dalam timezone pelabuhan, convert ke UTC untuk database
                            var waktuLokal = new DateTime(dateToCreate.Year, dateToCreate.Month, dateToCreate.Day, jam, menit, 0);
                            var waktuBerangkat = waktuLokal.AddHours(-timezoneOffsetHours);
                            var waktuTiba = waktuBerangkat.AddHours(durasiJam).AddMinutes(durasiMenit);
                            jadwals.Add(new Jadwal
                            {
                                pelabuhan_asal_id = pelabuhan_asal_id,
                                pelabuhan_tujuan_id = pelabuhan_tujuan_id,
                                kapal_id = kapal_id,
                                waktu_berangkat = waktuBerangkat,
                                waktu_tiba = waktuTiba,
                                kelas_layanan = kelas_layanan,
                                status = status,
                                grup_kendaraan_id = grupKendaraanId
                            });
                        }
                        else
                        {
                            if (!invalidRows.Contains(row.No))
                                invalidRows.Add(row.No);
                        }
                    }
                }

                if (invalidRows.Count > 0)
                {
                    MessageBox.Show($"Baris dengan format waktu tidak valid: {string.Join(", ", invalidRows)}\nFormat harus HH:mm (contoh: 08:00, 14:30)", 
                        "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (jadwals.Count == 0)
                {
                    MessageBox.Show("Tidak ada jadwal yang valid untuk disimpan!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = await _jadwalService.BulkCreateJadwalAsync(jadwals);
                var totalDates = datesToCreate.Count;
                var totalTimeSlots = _bulkTimeRows.Count(r => !string.IsNullOrWhiteSpace(r.JamBerangkat));
                MessageBox.Show(
                    $"{result.message}\n\nGrup: \"{grupNama}\"\nTotal: {jadwals.Count} jadwal dibuat ({totalDates} tanggal × {totalTimeSlots} waktu)\n(Setiap jadwal memiliki 13 golongan kendaraan dalam 1 grup)", 
                    result.success ? "Success" : "Error",
                    MessageBoxButton.OK, result.success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (result.success)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnBulkSave.IsEnabled = true;
                btnBulkSave.Content = "💾 Bulk Save";
            }
        }

        private bool ValidateInput(bool skipTimeValidation = false)
        {
            if (cbPelabuhanAsal.SelectedValue == null)
            {
                MessageBox.Show("Pilih pelabuhan asal!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbPelabuhanTujuan.SelectedValue == null)
            {
                MessageBox.Show("Pilih pelabuhan tujuan!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if ((int)cbPelabuhanAsal.SelectedValue == (int)cbPelabuhanTujuan.SelectedValue)
            {
                MessageBox.Show("Pelabuhan asal dan tujuan tidak boleh sama!", "Validasi", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbKapal.SelectedValue == null)
            {
                MessageBox.Show("Pilih kapal!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!skipTimeValidation)
            {
                if (dpTanggalBerangkat.SelectedDate == null)
                {
                    MessageBox.Show("Pilih tanggal keberangkatan!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtJamBerangkat.Text) || 
                    !int.TryParse(txtJamBerangkat.Text, out int jam) || jam < 0 || jam > 23)
                {
                    MessageBox.Show("Jam berangkat harus 00-23!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtMenitBerangkat.Text) || 
                    !int.TryParse(txtMenitBerangkat.Text, out int menit) || menit < 0 || menit > 59)
                {
                    MessageBox.Show("Menit berangkat harus 00-59!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtDurasiJam.Text) || 
                    !int.TryParse(txtDurasiJam.Text, out int durasiJam) || durasiJam < 0)
                {
                    MessageBox.Show("Durasi jam harus >= 0!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtDurasiMenit.Text) || 
                    !int.TryParse(txtDurasiMenit.Text, out int durasiMenit) || durasiMenit < 0 || durasiMenit > 59)
                {
                    MessageBox.Show("Durasi menit harus 00-59!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (durasiJam == 0 && durasiMenit == 0)
                {
                    MessageBox.Show("Durasi perjalanan harus lebih dari 0!", "Validasi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            var newRow = new BulkTimeRow
            {
                No = _bulkTimeRows.Count + 1
            };
            _bulkTimeRows.Add(newRow);
        }

        private void BtnClearRows_Click(object sender, RoutedEventArgs e)
        {
            if (_bulkTimeRows.Count > 0)
            {
                var result = MessageBox.Show("Hapus semua baris?", "Konfirmasi", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _bulkTimeRows.Clear();
                }
            }
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.DataContext is BulkTimeRow row)
            {
                _bulkTimeRows.Remove(row);
                // Renumber remaining rows
                for (int i = 0; i < _bulkTimeRows.Count; i++)
                {
                    _bulkTimeRows[i].No = i + 1;
                }
            }
        }



        // Load semua golongan detail kendaraan (13 rows) ke DataGrid untuk input harga
        private void LoadDetailKendaraanGrid()
        {
            _detailKendaraanRows.Clear();
            
            // Loop semua enum JenisKendaraan
            foreach (JenisKendaraan jenis in Enum.GetValues(typeof(JenisKendaraan)))
            {
                var spec = DetailKendaraan.GetSpecificationByJenis(jenis);
                _detailKendaraanRows.Add(new DetailKendaraanInputRow
                {
                    JenisKendaraanEnum = jenis,
                    Golongan = GetGolonganDisplayName(jenis),
                    Bobot = spec.Bobot,
                    Deskripsi = spec.Deskripsi,
                    SpesifikasiUkuran = spec.SpesifikasiUkuran,
                    Harga = 0 // Default harga 0, user harus isi
                });
            }
        }

        private string GetGolonganDisplayName(JenisKendaraan jenis)
        {
            return jenis switch
            {
                JenisKendaraan.Jalan_Kaki => "Jalan Kaki",
                JenisKendaraan.Golongan_I => "Golongan I",
                JenisKendaraan.Golongan_II => "Golongan II",
                JenisKendaraan.Golongan_III => "Golongan III",
                JenisKendaraan.Golongan_IV_A => "Golongan IV-A",
                JenisKendaraan.Golongan_IV_B => "Golongan IV-B",
                JenisKendaraan.Golongan_V_A => "Golongan V-A",
                JenisKendaraan.Golongan_V_B => "Golongan V-B",
                JenisKendaraan.Golongan_VI_A => "Golongan VI-A",
                JenisKendaraan.Golongan_VI_B => "Golongan VI-B",
                JenisKendaraan.Golongan_VII => "Golongan VII",
                JenisKendaraan.Golongan_VIII => "Golongan VIII",
                JenisKendaraan.Golongan_IX => "Golongan IX",
                _ => jenis.ToString()
            };
        }



        // Numeric only input untuk harga
        private void TxtNumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            return text.All(char.IsDigit);
        }

        private void DgBulkWaktu_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var row = e.Row.Item as BulkTimeRow;
                if (row != null && e.Column.Header.ToString() == "Jam Berangkat")
                {
                    var textBox = e.EditingElement as System.Windows.Controls.TextBox;
                    if (textBox != null)
                    {
                        var jamBerangkat = textBox.Text.Trim();
                        
                        // Validate HH:mm format
                        var parts = jamBerangkat.Split(':');
                        if (parts.Length == 2 && 
                            int.TryParse(parts[0], out int jam) && jam >= 0 && jam <= 23 &&
                            int.TryParse(parts[1], out int menit) && menit >= 0 && menit <= 59)
                        {
                            row.JamBerangkat = jamBerangkat;
                            // Calculate arrival time using current duration values
                            if (!string.IsNullOrWhiteSpace(txtDurasiJam.Text) && !string.IsNullOrWhiteSpace(txtDurasiMenit.Text))
                            {
                                row.CalculateJamTiba(txtDurasiJam, txtDurasiMenit);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(jamBerangkat))
                        {
                            MessageBox.Show("Format harus HH:mm (contoh: 08:00, 14:30)", "Format Tidak Valid",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            textBox.Text = row.JamBerangkat; // Revert to previous value
                        }
                    }
                }
            }
        }
        private void DgDetailKendaraan_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var row = e.Row.Item as DetailKendaraanInputRow;
                if (row != null && e.Column.Header.ToString() == "Harga (Rp)")
                {
                    var textBox = e.EditingElement as System.Windows.Controls.TextBox;
                    if (textBox != null)
                    {
                        var hargaText = textBox.Text.Trim().Replace(",", "").Replace(".", "");
                        
                        // Validate numeric
                        if (decimal.TryParse(hargaText, out decimal harga) && harga >= 0)
                        {
                            row.Harga = harga;
                        }
                        else if (!string.IsNullOrWhiteSpace(hargaText))
                        {
                            MessageBox.Show("Harga harus berupa angka positif!", "Format Tidak Valid",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            textBox.Text = row.Harga.ToString("N0"); // Revert to previous value
                        }
                    }
                }
            }
        }


    }

    // Helper class for bulk time input
    public class BulkTimeRow : INotifyPropertyChanged
    {
        private int _no;
        private string _jamBerangkat = "";
        private string _jamTiba = "";

        public int No
        {
            get => _no;
            set
            {
                _no = value;
                OnPropertyChanged(nameof(No));
            }
        }

        public string JamBerangkat
        {
            get => _jamBerangkat;
            set
            {
                _jamBerangkat = value;
                OnPropertyChanged(nameof(JamBerangkat));
            }
        }

        public string JamTiba
        {
            get => _jamTiba;
            set
            {
                _jamTiba = value;
                OnPropertyChanged(nameof(JamTiba));
            }
        }

        public void CalculateJamTiba(System.Windows.Controls.TextBox txtDurasiJam, System.Windows.Controls.TextBox txtDurasiMenit)
        {
            if (string.IsNullOrWhiteSpace(JamBerangkat))
            {
                JamTiba = "";
                return;
            }
            var parts = JamBerangkat.Split(':');
            if (parts.Length != 2 || 
                !int.TryParse(parts[0], out int jamBerangkat) ||
                !int.TryParse(parts[1], out int menitBerangkat))
            {
                JamTiba = "";
                return;
            }
            if (!int.TryParse(txtDurasiJam?.Text, out int durasiJam) || durasiJam < 0)
                durasiJam = 0;
            
            if (!int.TryParse(txtDurasiMenit?.Text, out int durasiMenit) || durasiMenit < 0 || durasiMenit > 59)
                durasiMenit = 0;

            if (durasiJam == 0 && durasiMenit == 0)
            {
                JamTiba = "Isi durasi dulu";
                return;
            }

            // Calculate arrival time
            var berangkat = new DateTime(2000, 1, 1, jamBerangkat, menitBerangkat, 0);
            var tiba = berangkat.AddHours(durasiJam).AddMinutes(durasiMenit);
            
            JamTiba = tiba.ToString("HH:mm");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }



    // Helper class untuk input detail kendaraan dengan harga per golongan
    public class DetailKendaraanInputRow : INotifyPropertyChanged
    {
        public JenisKendaraan JenisKendaraanEnum { get; set; }
        public string Golongan { get; set; } = string.Empty;
        public int Bobot { get; set; }
        public string Deskripsi { get; set; } = string.Empty;
        public string SpesifikasiUkuran { get; set; } = string.Empty;
        
        private decimal _harga;
        public decimal Harga
        {
            get => _harga;
            set
            {
                if (_harga != value)
                {
                    _harga = value;
                    OnPropertyChanged(nameof(Harga));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
