using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminJadwalFormDialog : Window
    {
        private readonly JadwalService _jadwalService;
        private readonly PelabuhanService _pelabuhanService;
        private readonly KapalService _kapalService;
        private Jadwal? _existingJadwal;
        private bool _isEditMode;
        private ObservableCollection<BulkTimeRow> _bulkTimeRows;
        private ObservableCollection<DateTime> _selectedDates;
        private bool _isDragging = false;
        private DateTime? _dragStartDate = null;

        public AdminJadwalFormDialog(Jadwal? jadwal = null)
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            _pelabuhanService = new PelabuhanService();
            _kapalService = new KapalService();
            _existingJadwal = jadwal;
            _isEditMode = jadwal != null;

            // Initialize bulk time DataGrid
            _bulkTimeRows = new ObservableCollection<BulkTimeRow>();
            dgBulkWaktu.ItemsSource = _bulkTimeRows;

            // Initialize selected dates
            _selectedDates = new ObservableCollection<DateTime>();
            icSelectedDates.ItemsSource = _selectedDates;

            LoadInitialData();

            if (_isEditMode && jadwal != null)
            {
                txtTitle.Text = "Edit Jadwal";
                btnSave.Content = "Update";
                btnBulkSave.Visibility = Visibility.Collapsed;
                dgBulkWaktu.IsEnabled = false;
                btnAddRow.IsEnabled = false;
                btnClearRows.IsEnabled = false;
                calendarMultiSelect.IsEnabled = false;
                btnClearDates.IsEnabled = false;
                LoadJadwalData(jadwal);
            }
        }

        private async void LoadInitialData()
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
                // Remove old handlers if exist to prevent duplicates
                button.PreviewMouseLeftButtonDown -= DayButton_PreviewMouseLeftButtonDown;
                button.PreviewMouseMove -= DayButton_PreviewMouseMove;
                button.PreviewMouseLeftButtonUp -= DayButton_PreviewMouseLeftButtonUp;
                
                // Add new handlers
                button.PreviewMouseLeftButtonDown += DayButton_PreviewMouseLeftButtonDown;
                button.PreviewMouseMove += DayButton_PreviewMouseMove;
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
                    dayButton.CaptureMouse();
                    
                    // Toggle the clicked date
                    if (!_selectedDates.Contains(date))
                    {
                        _selectedDates.Add(date);
                        UpdateDayButtonStyles();
                    }
                }
            }
        }

        private void DayButton_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is System.Windows.Controls.Primitives.CalendarDayButton dayButton &&
                    dayButton.DataContext is DateTime hoverDate)
                {
                    var date = hoverDate.Date;
                    
                    // Add to selection if not already selected
                    if (!_selectedDates.Contains(date))
                    {
                        _selectedDates.Add(date);
                        
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
                
                if (sender is System.Windows.Controls.Primitives.CalendarDayButton dayButton)
                {
                    dayButton.ReleaseMouseCapture();
                }
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
                // Remove from collection
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

                // Parse time
                var jam = int.Parse(txtJamBerangkat.Text);
                var menit = int.Parse(txtMenitBerangkat.Text);
                var durasiJam = int.Parse(txtDurasiJam.Text);
                var durasiMenit = int.Parse(txtDurasiMenit.Text);

                if (_isEditMode && _existingJadwal != null)
                {
                    // Update mode - use existing date
                    var tanggal = _existingJadwal.waktu_berangkat.Date;
                    var waktu_berangkat = new DateTime(tanggal.Year, tanggal.Month, tanggal.Day, jam, menit, 0, DateTimeKind.Utc);
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
                    // Create mode - use selected dates
                    if (_selectedDates.Count == 0)
                    {
                        MessageBox.Show("Pilih minimal satu tanggal terlebih dahulu!", "Validasi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        btnSave.IsEnabled = true;
                        btnSave.Content = "💾 Simpan";
                        return;
                    }
                    
                    var jadwals = new List<Jadwal>();
                    
                    // Create jadwal for each selected date
                    foreach (var dateToCreate in _selectedDates)
                    {
                        var waktu_berangkat_for_date = new DateTime(
                            dateToCreate.Year, dateToCreate.Month, dateToCreate.Day, 
                            jam, menit, 0, DateTimeKind.Utc);
                        var waktu_tiba_for_date = waktu_berangkat_for_date.AddHours(durasiJam).AddMinutes(durasiMenit);
                        
                        jadwals.Add(new Jadwal
                        {
                            pelabuhan_asal_id = pelabuhan_asal_id,
                            pelabuhan_tujuan_id = pelabuhan_tujuan_id,
                            kapal_id = kapal_id,
                            waktu_berangkat = waktu_berangkat_for_date,
                            waktu_tiba = waktu_tiba_for_date,
                            kelas_layanan = kelas_layanan,
                            status = status
                        });
                    }

                    if (jadwals.Count == 1)
                    {
                        var result = await _jadwalService.CreateJadwalAsync(jadwals[0]);
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
                        var result = await _jadwalService.BulkCreateJadwalAsync(jadwals);
                        MessageBox.Show(result.message, result.success ? "Success" : "Error",
                            MessageBoxButton.OK, result.success ? MessageBoxImage.Information : MessageBoxImage.Error);

                        if (result.success)
                        {
                            DialogResult = true;
                            Close();
                        }
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
                            var waktuBerangkat = new DateTime(dateToCreate.Year, dateToCreate.Month, dateToCreate.Day, jam, menit, 0, DateTimeKind.Utc);
                            var waktuTiba = waktuBerangkat.AddHours(durasiJam).AddMinutes(durasiMenit);

                            jadwals.Add(new Jadwal
                            {
                                pelabuhan_asal_id = pelabuhan_asal_id,
                                pelabuhan_tujuan_id = pelabuhan_tujuan_id,
                                kapal_id = kapal_id,
                                waktu_berangkat = waktuBerangkat,
                                waktu_tiba = waktuTiba,
                                kelas_layanan = kelas_layanan,
                                status = status
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
                MessageBox.Show(result.message, result.success ? "Success" : "Error",
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

            // Parse jam berangkat
            var parts = JamBerangkat.Split(':');
            if (parts.Length != 2 || 
                !int.TryParse(parts[0], out int jamBerangkat) ||
                !int.TryParse(parts[1], out int menitBerangkat))
            {
                JamTiba = "";
                return;
            }

            // Parse durasi
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
}
