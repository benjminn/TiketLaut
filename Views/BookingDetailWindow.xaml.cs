using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TiketLaut.Models;

namespace TiketLaut.Views
{
    public partial class BookingDetailWindow : Window
    {
        private bool _isFromSchedule = false;
        private ScheduleItem? _selectedSchedule;
        private SearchCriteria? _searchCriteria;
        private Window? _parentWindow;

        public BookingDetailWindow()
        {
            InitializeComponent();
            ApplyResponsiveLayout();

            // NavbarSelainHomepage tidak memerlukan SetUserInfo karena hanya menampilkan logo
        }

        // Constructor dengan parent window parameter
        public BookingDetailWindow(bool isFromSchedule, Window? parentWindow = null) : this()
        {
            _isFromSchedule = isFromSchedule;
            _parentWindow = parentWindow;
        }

        /// <summary>
        /// Method untuk set data schedule yang dipilih dari ScheduleWindow
        /// </summary>
        public void SetScheduleData(ScheduleItem scheduleItem)
        {
            _selectedSchedule = scheduleItem;
            System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] SetScheduleData called with: {scheduleItem?.FerryType}");

            // Update UI dengan data schedule
            UpdateUIWithScheduleData();
        }

        public void SetSearchCriteria(SearchCriteria searchCriteria)
        {
            _searchCriteria = searchCriteria;
            System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] SetSearchCriteria called");
            System.Diagnostics.Debug.WriteLine($"  JenisKendaraanId: {searchCriteria.JenisKendaraanId}");
            System.Diagnostics.Debug.WriteLine($"  JumlahPenumpang: {searchCriteria.JumlahPenumpang}");

            // Generate form penumpang sesuai jumlah
            GeneratePassengerForms();

            // Update detail kendaraan
            UpdateVehicleDetails();

            // Update vehicle and passenger display specifically
            UpdateVehicleAndPassengerDisplay();

            // Update price details
            UpdatePriceDetails();
        }

        private void UpdateVehicleAndPassengerDisplay()
        {
            if (_searchCriteria == null) return;

            string jenisKendaraanText = GetJenisKendaraanText(_searchCriteria.JenisKendaraanId);
            string penumpangText = $"Dewasa ({_searchCriteria.JumlahPenumpang}x)";

            System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] UpdateVehicleAndPassengerDisplay:");
            System.Diagnostics.Debug.WriteLine($"  jenisKendaraanText: {jenisKendaraanText}");
            System.Diagnostics.Debug.WriteLine($"  penumpangText: {penumpangText}");

            // Update all vehicle type displays
            UpdateVehicleTypeDisplays(jenisKendaraanText);

            // Update all passenger count displays
            UpdatePassengerCountDisplays(penumpangText);
        }

        private void UpdatePassengerCountDisplays(string penumpangText)
        {
            // Update price breakdown sections
            var txtPassengerPrice = FindName("txtPassengerPrice") as TextBlock;
            if (txtPassengerPrice != null)
            {
                txtPassengerPrice.Text = penumpangText;
            }

            var txtSidebarPassengerCount = FindName("txtSidebarPassengerCount") as TextBlock;
            if (txtSidebarPassengerCount != null)
            {
                txtSidebarPassengerCount.Text = penumpangText;
            }

            System.Diagnostics.Debug.WriteLine($"Updated all passenger count displays to: {penumpangText}");
        }

        private void UpdateVehicleTypeDisplays(string jenisKendaraanText)
        {
            // Update main vehicle section
            if (txtVehicleType != null)
            {
                txtVehicleType.Text = jenisKendaraanText;
            }

            if (txtVehicleInfo != null)
            {
                txtVehicleInfo.Text = jenisKendaraanText;
            }

            // Update price breakdown sections
            var txtVehicleTypePrice = FindName("txtVehicleTypePrice") as TextBlock;
            if (txtVehicleTypePrice != null)
            {
                txtVehicleTypePrice.Text = jenisKendaraanText;
            }

            var txtSidebarVehicleType = FindName("txtSidebarVehicleType") as TextBlock;
            if (txtSidebarVehicleType != null)
            {
                txtSidebarVehicleType.Text = jenisKendaraanText;
            }

            System.Diagnostics.Debug.WriteLine($"Updated all vehicle type displays to: {jenisKendaraanText}");
        }

        private void UpdateUIWithScheduleData()
        {
            if (_selectedSchedule == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("[BookingDetailWindow] UpdateUIWithScheduleData started");

                // Update Right Section - Ferry Info
                if (txtFerryType != null)
                {
                    txtFerryType.Text = _selectedSchedule.FerryType;
                    System.Diagnostics.Debug.WriteLine($"Updated txtFerryType: {_selectedSchedule.FerryType}");
                }

                // Update pelabuhan asal dan tujuan
                if (txtDeparture != null)
                {
                    txtDeparture.Text = _selectedSchedule.DeparturePort;
                    System.Diagnostics.Debug.WriteLine($"Updated txtDeparture: {_selectedSchedule.DeparturePort}");
                }

                if (txtArrival != null)
                {
                    txtArrival.Text = _selectedSchedule.ArrivalPort;
                    System.Diagnostics.Debug.WriteLine($"Updated txtArrival: {_selectedSchedule.ArrivalPort}");
                }

                // Update check-in time dan warning message
                if (txtCheckInTime != null)
                {
                    // Parse departure time dan kurangi 15 menit untuk check-in
                    if (TimeSpan.TryParse(_selectedSchedule.DepartureTime, out TimeSpan departureTime))
                    {
                        var checkInTime = departureTime.Subtract(TimeSpan.FromMinutes(15));
                        txtCheckInTime.Text = $"{_selectedSchedule.BoardingDate} - {checkInTime:hh\\:mm}";

                        // Update warning message dengan waktu yang sama
                        var txtWarningMessage = FindName("txtWarningMessage") as TextBlock;
                        if (txtWarningMessage != null)
                        {
                            txtWarningMessage.Text = $"E-Tiket akan hangus bila anda belum masuk ke pelabuhan setelah {checkInTime:hh\\:mm}";
                        }
                    }
                    else
                    {
                        txtCheckInTime.Text = $"{_selectedSchedule.BoardingDate} - {_selectedSchedule.DepartureTime}";

                        var txtWarningMessage = FindName("txtWarningMessage") as TextBlock;
                        if (txtWarningMessage != null)
                        {
                            txtWarningMessage.Text = $"E-Tiket akan hangus bila anda belum masuk ke pelabuhan setelah {_selectedSchedule.DepartureTime}";
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"Updated txtCheckInTime: {txtCheckInTime.Text}");
                }

                // Update price displays immediately
                UpdateAllPriceDisplays(_selectedSchedule.Price);

                // Update vehicle and passenger details if search criteria is available
                if (_searchCriteria != null)
                {
                    UpdateVehicleAndPassengerDisplay();
                }

                System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] UpdateUIWithScheduleData completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] Error updating UI: {ex.Message}");
            }
        }

        private void UpdateAllPriceDisplays(string price)
        {
            System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] UpdateAllPriceDisplays called with: {price}");

            try
            {
                // Update main collapsed price display
                if (txtTotalHargaCollapsed != null)
                {
                    txtTotalHargaCollapsed.Text = price;
                    System.Diagnostics.Debug.WriteLine($"Updated txtTotalHargaCollapsed: {price}");
                }

                // Update expanded detail prices in main section
                if (txtSidebarPrice != null)
                {
                    txtSidebarPrice.Text = price;
                    System.Diagnostics.Debug.WriteLine($"Updated txtSidebarPrice: {price}");
                }

                // Update all specific named TextBlocks for prices
                UpdateNamedPriceElements(price);

                // Update all other TextBlocks that contain hardcoded price
                UpdateAllPriceTextBlocks(price);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] Error updating prices: {ex.Message}");
            }
        }

        private void UpdateNamedPriceElements(string price)
        {
            // Update expanded total price in main section
            var txtTotalHargaExpanded = FindName("txtTotalHargaExpanded") as TextBlock;
            if (txtTotalHargaExpanded != null)
            {
                txtTotalHargaExpanded.Text = price;
                System.Diagnostics.Debug.WriteLine($"Updated txtTotalHargaExpanded: {price}");
            }

            // Update sidebar total price (collapsed state)
            var txtSidebarTotalPrice = FindName("txtSidebarTotalPrice") as TextBlock;
            if (txtSidebarTotalPrice != null)
            {
                txtSidebarTotalPrice.Text = price;
                System.Diagnostics.Debug.WriteLine($"Updated txtSidebarTotalPrice: {price}");
            }

            // Update sidebar detail price (expanded state)
            var txtSidebarDetailPrice = FindName("txtSidebarDetailPrice") as TextBlock;
            if (txtSidebarDetailPrice != null)
            {
                txtSidebarDetailPrice.Text = price;
                System.Diagnostics.Debug.WriteLine($"Updated txtSidebarDetailPrice: {price}");
            }

            // Update sidebar final price (expanded state)
            var txtSidebarFinalPrice = FindName("txtSidebarFinalPrice") as TextBlock;
            if (txtSidebarFinalPrice != null)
            {
                txtSidebarFinalPrice.Text = price;
                System.Diagnostics.Debug.WriteLine($"Updated txtSidebarFinalPrice: {price}");
            }
        }

        private void UpdateAllPriceTextBlocks(string price)
        {
            // Find all TextBlocks in the visual tree and update those with price
            var allTextBlocks = FindVisualChildren<TextBlock>(this);

            foreach (var textBlock in allTextBlocks)
            {
                if (textBlock.Text.Contains("IDR") &&
                    (textBlock.Text.Contains("487") || textBlock.Text.Contains("487.853")))
                {
                    textBlock.Text = price;
                    System.Diagnostics.Debug.WriteLine($"Updated price TextBlock via FindVisual: {price}");
                }
            }
        }

        private void UpdatePriceBreakdownTexts(string jenisKendaraanText)
        {
            if (_searchCriteria == null) return;

            string penumpangText = $"Dewasa ({_searchCriteria.JumlahPenumpang}x)";

            // Call the specific update methods
            UpdateVehicleTypeDisplays(jenisKendaraanText);
            UpdatePassengerCountDisplays(penumpangText);

            // Also update via visual tree search as fallback
            var allTextBlocks = FindVisualChildren<TextBlock>(this);
            foreach (var textBlock in allTextBlocks)
            {
                if (textBlock.Text.Contains("Sepeda Motor") && textBlock.Text.Contains("500cc") &&
                    !textBlock.Text.Equals(jenisKendaraanText))
                {
                    textBlock.Text = jenisKendaraanText;
                    System.Diagnostics.Debug.WriteLine($"Updated vehicle text via fallback: {jenisKendaraanText}");
                }
                else if (textBlock.Text.Contains("Dewasa") && textBlock.Text.Contains("x") &&
                         !textBlock.Text.Equals(penumpangText))
                {
                    textBlock.Text = penumpangText;
                    System.Diagnostics.Debug.WriteLine($"Updated passenger text via fallback: {penumpangText}");
                }
            }
        }

        private void GeneratePassengerForms()
        {
            if (_searchCriteria == null) return;

            int jumlahPenumpang = _searchCriteria.JumlahPenumpang;
            System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] GeneratePassengerForms for {jumlahPenumpang} passengers");

            // 1. Show/Hide first 3 passenger forms based on count
            for (int i = 1; i <= 3; i++)
            {
                var passengerBorder = FindName($"borderPassenger{i}") as Border;
                if (passengerBorder != null)
                {
                    bool shouldShow = i <= jumlahPenumpang;
                    passengerBorder.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine($"Passenger {i} border visibility: {passengerBorder.Visibility}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"borderPassenger{i} not found!");
                }
            }

            // 2. Generate additional forms if more than 3 passengers
            var additionalContainer = FindName("additionalPassengerFormsContainer") as StackPanel;
            if (additionalContainer != null)
            {
                // Clear existing additional forms
                additionalContainer.Children.Clear();

                // Generate forms for passengers 4 and beyond
                if (jumlahPenumpang > 3)
                {
                    for (int i = 4; i <= jumlahPenumpang; i++)
                    {
                        Border passengerForm = CreatePassengerForm(i);
                        additionalContainer.Children.Add(passengerForm);
                    }
                    System.Diagnostics.Debug.WriteLine($"Generated {jumlahPenumpang - 3} additional passenger forms");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("additionalPassengerFormsContainer not found!");
            }
        }

        private Border CreatePassengerForm(int passengerNumber)
        {
            // Create main border
            Border borderPassenger = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(17),
                Padding = new Thickness(25, 20, 25, 20),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D")),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 0, 0, 16)
            };

            // Create main StackPanel
            StackPanel mainStack = new StackPanel();

            // Create toggle button header
            Button toggleButton = new Button
            {
                Tag = passengerNumber.ToString(),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 16)
            };
            toggleButton.Click += BtnTogglePassenger_Click;

            // Create grid for toggle button content
            Grid toggleGrid = new Grid();
            toggleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toggleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock headerText = new TextBlock
            {
                Text = $"Penumpang {passengerNumber}",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#042769")),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(headerText, 0);
            toggleGrid.Children.Add(headerText);

            Image toggleIcon = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Views/Assets/Icons/icondropdowngelap.png")),
                Width = 20,
                Height = 20,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(0)
            };
            Grid.SetColumn(toggleIcon, 1);
            toggleGrid.Children.Add(toggleIcon);

            toggleButton.Content = toggleGrid;
            mainStack.Children.Add(toggleButton);

            // Create expandable content panel
            StackPanel contentPanel = new StackPanel
            {
                Visibility = Visibility.Collapsed
            };

            // Add title
            TextBlock infoTitle = new TextBlock
            {
                Text = "Info Identitas Penumpang",
                FontSize = 16,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#042769")),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 16)
            };
            contentPanel.Children.Add(infoTitle);

            // Add gender selection
            StackPanel genderPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 16)
            };

            RadioButton rbTuan = new RadioButton
            {
                Content = "Tuan",
                GroupName = $"Gender{passengerNumber}",
                IsChecked = true,
                Style = FindResource("CustomRadioButton") as Style,
                Margin = new Thickness(0, 0, 16, 0)
            };
            genderPanel.Children.Add(rbTuan);

            RadioButton rbNyonya = new RadioButton
            {
                Content = "Nyonya",
                GroupName = $"Gender{passengerNumber}",
                Style = FindResource("CustomRadioButton") as Style,
                Margin = new Thickness(0, 0, 16, 0)
            };
            genderPanel.Children.Add(rbNyonya);

            RadioButton rbNona = new RadioButton
            {
                Content = "Nona",
                GroupName = $"Gender{passengerNumber}",
                Style = FindResource("CustomRadioButton") as Style
            };
            genderPanel.Children.Add(rbNona);

            contentPanel.Children.Add(genderPanel);

            // Add name field with floating label
            StackPanel namePanel = CreateFloatingLabelField(
                $"txtNamaPassenger{passengerNumber}",
                "Nama lengkap sesuai KTP/Paspor/SIM",
                "Sesuai KTP/Paspor/KK tanpa tanda baca dan gelar"
            );
            contentPanel.Children.Add(namePanel);

            // Add identity section
            Grid identityGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
            identityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 150 });
            identityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            identityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Identity type ComboBox
            Grid identityTypeGrid = CreateFloatingLabelComboBoxField(
                $"cmbIdentitas{passengerNumber}",
                "Identitas",
                new string[] { "KTP", "Paspor", "SIM", "KK" }
            );
            Grid.SetColumn(identityTypeGrid, 0);
            identityGrid.Children.Add(identityTypeGrid);

            // Identity number TextBox
            Grid identityNumberGrid = CreateFloatingLabelTextBoxGrid(
                $"txtIdPassenger{passengerNumber}",
                "Nomor Identitas"
            );
            Grid.SetColumn(identityNumberGrid, 2);
            identityGrid.Children.Add(identityNumberGrid);

            contentPanel.Children.Add(identityGrid);

            mainStack.Children.Add(contentPanel);
            borderPassenger.Child = mainStack;

            return borderPassenger;
        }

        private Grid CreateFloatingLabelTextBoxGrid(string textBoxName, string labelText)
        {
            Grid grid = new Grid();

            Border inputBorder = new Border
            {
                Style = FindResource("FloatingLabelTextBox") as Style
            };

            Grid innerGrid = new Grid();
            TextBox textBox = new TextBox
            {
                Height = 52,
                Padding = new Thickness(16, 0, 16, 0),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#042769")),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            textBox.GotFocus += FloatingTextBox_GotFocus;
            textBox.LostFocus += FloatingTextBox_LostFocus;
            textBox.TextChanged += FloatingTextBox_TextChanged;

            innerGrid.Children.Add(textBox);
            inputBorder.Child = innerGrid;
            grid.Children.Add(inputBorder);

            Border labelBorder = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(4, 0, 4, 0),
                IsHitTestVisible = false
            };

            TextBlock label = new TextBlock
            {
                Text = labelText,
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
            };

            labelBorder.Child = label;
            grid.Children.Add(labelBorder);

            return grid;
        }

        private StackPanel CreateFloatingLabelField(string fieldName, string labelText, string? helperText)
        {
            StackPanel panel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

            Grid grid = new Grid();

            Border inputBorder = new Border
            {
                Style = FindResource("FloatingLabelTextBox") as Style
            };

            Grid innerGrid = new Grid();
            TextBox textBox = new TextBox
            {
                Height = 52,
                Padding = new Thickness(16, 0, 16, 0),
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#042769")),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            textBox.GotFocus += FloatingTextBox_GotFocus;
            textBox.LostFocus += FloatingTextBox_LostFocus;
            textBox.TextChanged += FloatingTextBox_TextChanged;

            innerGrid.Children.Add(textBox);
            inputBorder.Child = innerGrid;
            grid.Children.Add(inputBorder);

            Border labelBorder = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(4, 0, 4, 0),
                IsHitTestVisible = false
            };

            TextBlock label = new TextBlock
            {
                Text = labelText,
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
            };

            labelBorder.Child = label;
            grid.Children.Add(labelBorder);

            panel.Children.Add(grid);

            if (!string.IsNullOrEmpty(helperText))
            {
                TextBlock helper = new TextBlock
                {
                    Text = helperText,
                    FontSize = 11,
                    FontWeight = FontWeights.Regular,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                panel.Children.Add(helper);
            }

            return panel;
        }

        private Grid CreateFloatingLabelComboBoxField(string comboBoxName, string labelText, string[] items)
        {
            Grid grid = new Grid();

            Border inputBorder = new Border
            {
                Style = FindResource("FloatingLabelTextBox") as Style
            };

            ComboBox comboBox = new ComboBox
            {
                Height = 52,
                Padding = new Thickness(16, 0, 16, 0),
                Style = FindResource("FloatingLabelComboBox") as Style
            };
            comboBox.GotFocus += FloatingComboBox_GotFocus;
            comboBox.LostFocus += FloatingComboBox_LostFocus;
            comboBox.SelectionChanged += FloatingComboBox_SelectionChanged;

            foreach (string item in items)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = item });
            }

            inputBorder.Child = comboBox;
            grid.Children.Add(inputBorder);

            Border labelBorder = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(4, 0, 4, 0),
                IsHitTestVisible = false
            };

            TextBlock label = new TextBlock
            {
                Text = labelText,
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
            };

            labelBorder.Child = label;
            grid.Children.Add(labelBorder);

            return grid;
        }

        // Helper method to find first child control of type T
        private T? FindChildControl<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var childResult = FindChildControl<T>(child);
                if (childResult != null)
                {
                    return childResult;
                }
            }
            return null;
        }

        // Helper method to find all child controls of type T
        private List<T> FindChildControls<T>(DependencyObject parent) where T : DependencyObject
        {
            var results = new List<T>();
            if (parent == null) return results;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    results.Add(result);
                }

                results.AddRange(FindChildControls<T>(child));
            }
            return results;
        }

        private void UpdateVehicleDetails()
        {
            if (_searchCriteria == null) return;

            string jenisKendaraanText = GetJenisKendaraanText(_searchCriteria.JenisKendaraanId);
            string kapasitasText = GetKapasitasText(_searchCriteria.JenisKendaraanId, _searchCriteria.JumlahPenumpang);

            System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] UpdateVehicleDetails:");
            System.Diagnostics.Debug.WriteLine($"  jenisKendaraanText: {jenisKendaraanText}");
            System.Diagnostics.Debug.WriteLine($"  kapasitasText: {kapasitasText}");

            // Update vehicle type display
            if (txtVehicleType != null)
            {
                txtVehicleType.Text = jenisKendaraanText;
                System.Diagnostics.Debug.WriteLine($"Updated txtVehicleType: {jenisKendaraanText}");
            }

            if (txtVehicleCapacity != null)
            {
                txtVehicleCapacity.Text = kapasitasText;
                System.Diagnostics.Debug.WriteLine($"Updated txtVehicleCapacity: {kapasitasText}");
            }

            if (txtVehicleInfo != null)
            {
                txtVehicleInfo.Text = jenisKendaraanText;
                System.Diagnostics.Debug.WriteLine($"Updated txtVehicleInfo: {jenisKendaraanText}");
            }

            // Show/Hide vehicle section berdasarkan jenis kendaraan
            if (vehicleSection != null)
            {
                // Hide jika pejalan kaki (0) atau sepeda (1) - tidak perlu plat nomor
                bool shouldShow = _searchCriteria.JenisKendaraanId > 1;
                vehicleSection.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"Vehicle section visibility: {vehicleSection.Visibility} (JenisKendaraanId: {_searchCriteria.JenisKendaraanId})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("vehicleSection not found!");
            }

            // Update price breakdown texts
            UpdatePriceBreakdownTexts(jenisKendaraanText);
        }

        private void UpdatePriceDetails()
        {
            if (_selectedSchedule == null || _searchCriteria == null) return;

            try
            {
                // Parse harga dari schedule
                string priceText = _selectedSchedule.Price;
                System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] UpdatePriceDetails with price: {priceText}");

                // Update semua tampilan harga
                UpdateAllPriceDisplays(priceText);

                // Update breakdown text
                string jenisKendaraanText = GetJenisKendaraanText(_searchCriteria.JenisKendaraanId);
                UpdatePriceBreakdownTexts(jenisKendaraanText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] Error updating price: {ex.Message}");
            }
        }

        private string GetJenisKendaraanText(int jenisKendaraanId)
        {
            return jenisKendaraanId switch
            {
                0 => "Pejalan kaki tanpa kendaraan",
                1 => "Sepeda",
                2 => "Sepeda Motor (<500cc)",
                3 => "Sepeda Motor (>500cc) (Golongan III)",
                4 => "Mobil jeep, sedan, minibus",
                5 => "Mobil barang bak muatan",
                6 => "Mobil bus penumpang (5-7 meter)",
                7 => "Mobil barang (truk/tangki) ukuran sedang",
                8 => "Mobil bus penumpang (7-10 meter)",
                9 => "Mobil barang (truk/tangki) sedang",
                10 => "Mobil tronton, tangki, penarik + gandengan (10-12 meter)",
                11 => "Mobil tronton, tangki, alat berat (12-16 meter)",
                12 => "Mobil tronton, tangki, alat berat (>16 meter)",
                _ => "Kendaraan tidak diketahui"
            };
        }

        private string GetKapasitasText(int jenisKendaraanId, int jumlahPenumpang)
        {
            if (jenisKendaraanId == 0) // Pejalan kaki
            {
                return $"{jumlahPenumpang} Penumpang";
            }
            else if (jenisKendaraanId <= 3) // Motor
            {
                return $"Golongan III (Maks. {Math.Min(jumlahPenumpang, 3)} Penumpang)";
            }
            else if (jenisKendaraanId <= 5) // Mobil kecil
            {
                return $"Golongan IV (Maks. {Math.Min(jumlahPenumpang, 7)} Penumpang)";
            }
            else // Kendaraan besar
            {
                return $"Golongan V+ (Maks. {jumlahPenumpang} Penumpang)";
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    if (child != null)
                    {
                        foreach (T childOfChild in FindVisualChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }

        private T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            T? foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                T? childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            if (this.IsLoaded && MainContentGrid != null)
            {
                double windowWidth = this.ActualWidth;

                if (windowWidth < 1400)
                {
                    MainContentGrid.Margin = new Thickness(60, 20, 60, 40);
                }
                else if (windowWidth < 1600)
                {
                    MainContentGrid.Margin = new Thickness(80, 25, 80, 45);
                }
                else
                {
                    MainContentGrid.Margin = new Thickness(95, 30, 95, 50);
                }
            }
        }

        // TextBox Placeholder Logic
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string placeholder = textBox.Tag?.ToString() ?? "";

                // Jika text saat ini sama dengan placeholder, hapus dan ubah warna
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                }
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string placeholder = textBox.Tag?.ToString() ?? "";

                // Jika TextBox kosong saat kehilangan focus, kembalikan placeholder
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                }
            }
        }

        private bool IsPlaceholderText(TextBox textBox)
        {
            string placeholder = textBox.Tag?.ToString() ?? "";
            return textBox.Text == placeholder;
        }

        // Floating Label Logic
        private void FloatingTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    // Find the label's parent Border to change its VerticalAlignment
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Top;
                        labelBorder.Margin = new Thickness(12, 8, 0, 0);
                    }

                    // Animate label to float up
                    label.FontSize = 11;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));

                    // Adjust TextBox padding when focused
                    textBox.Padding = new Thickness(16, 16, 16, 8);
                }
            }
        }

        private void FloatingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    // Find the label's parent Border to reset its VerticalAlignment
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Center;
                        labelBorder.Margin = new Thickness(12, 0, 0, 0);
                    }

                    // Reset label if textbox is empty
                    label.FontSize = 14;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

                    // Reset TextBox padding
                    textBox.Padding = new Thickness(16, 0, 16, 0);
                }
            }
        }

        private void FloatingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;

                    // Keep label floated if there's text
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Top;
                            labelBorder.Margin = new Thickness(12, 8, 0, 0);
                        }
                        label.FontSize = 11;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));
                        textBox.Padding = new Thickness(16, 16, 16, 8);
                    }
                    else if (!textBox.IsFocused)
                    {
                        // Reset only if not focused and empty
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Center;
                            labelBorder.Margin = new Thickness(12, 0, 0, 0);
                        }
                        label.FontSize = 14;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                        textBox.Padding = new Thickness(16, 0, 16, 0);
                    }
                }
            }
        }

        // Floating Label Logic for ComboBox
        private void FloatingComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Top;
                        labelBorder.Margin = new Thickness(12, 8, 0, 0);
                    }

                    label.FontSize = 11;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));
                    comboBox.Padding = new Thickness(16, 16, 16, 8);
                }
            }
        }

        private void FloatingComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null && comboBox.SelectedIndex == -1)
                {
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Center;
                        labelBorder.Margin = new Thickness(12, 0, 0, 0);
                    }

                    label.FontSize = 14;
                    label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                    comboBox.Padding = new Thickness(16, 0, 16, 0);
                }
            }
        }

        private void FloatingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;

                    if (comboBox.SelectedIndex != -1)
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Top;
                            labelBorder.Margin = new Thickness(12, 8, 0, 0);
                        }
                        label.FontSize = 11;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D"));
                        comboBox.Padding = new Thickness(16, 16, 16, 8);
                    }
                    else if (!comboBox.IsFocused)
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Center;
                            labelBorder.Margin = new Thickness(12, 0, 0, 0);
                        }
                        label.FontSize = 14;
                        label.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                        comboBox.Padding = new Thickness(16, 0, 16, 0);
                    }
                }
            }
        }

        // PERBAIKAN METHOD KEMBALI
        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            if (_isFromSchedule && _parentWindow is ScheduleWindow scheduleWindow)
            {
                // Kembalikan ke window yang sudah ada (parent window)
                scheduleWindow.Show();
                scheduleWindow.WindowState = this.WindowState;
                scheduleWindow.Left = this.Left;
                scheduleWindow.Top = this.Top;
                scheduleWindow.Width = this.Width;
                scheduleWindow.Height = this.Height;

                this.Close();
            }
            else if (_isFromSchedule)
            {
                // Jika parent window tidak tersedia, buat ScheduleWindow baru dengan data yang sama
                var newScheduleWindow = new ScheduleWindow();

                // Copy window properties
                newScheduleWindow.Left = this.Left;
                newScheduleWindow.Top = this.Top;
                newScheduleWindow.Width = this.Width;
                newScheduleWindow.Height = this.Height;
                newScheduleWindow.WindowState = this.WindowState;

                newScheduleWindow.Show();
                this.Close();
            }
            else
            {
                // Jika tidak dari schedule, tutup window
                this.Close();
            }
        }

        private void BtnTogglePassenger_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            string passengerNumber = button.Tag?.ToString() ?? "1";

            // Find the panel and image icon
            var panel = this.FindName($"pnlPassenger{passengerNumber}") as StackPanel;
            var image = this.FindName($"pathTogglePassenger{passengerNumber}") as Image;

            if (panel != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    panel.Visibility = Visibility.Visible;
                    // Rotate arrow down (180 degrees) for up-down rotation
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    // Rotate arrow up (0 degrees)
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }
            }
        }

        private void BtnToggleDetailHarga_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the detail harga panel
            var panel = this.FindName("pnlDetailHarga") as StackPanel;
            var image = this.FindName("pathToggleDetailHarga") as Image;
            var txtHeader = this.FindName("txtHeaderHarga") as TextBlock;
            var txtPrice = this.FindName("txtTotalHargaCollapsed") as TextBlock;
            var borderSeparator = this.FindName("borderSeparatorCollapsed") as Border;

            if (panel != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    // Expand: Show detail
                    panel.Visibility = Visibility.Visible;
                    if (txtHeader != null) txtHeader.Text = "Detail Harga";
                    if (txtPrice != null) txtPrice.Visibility = Visibility.Collapsed;
                    if (borderSeparator != null) borderSeparator.Visibility = Visibility.Collapsed;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    // Collapse: Show total
                    panel.Visibility = Visibility.Collapsed;
                    if (txtHeader != null) txtHeader.Text = "Total Harga";
                    if (txtPrice != null) txtPrice.Visibility = Visibility.Visible;
                    if (borderSeparator != null) borderSeparator.Visibility = Visibility.Visible;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }

                // Update semua harga setelah toggle untuk memastikan sinkronisasi
                if (_selectedSchedule != null)
                {
                    UpdateAllPriceDisplays(_selectedSchedule.Price);
                }
            }
        }

        private void BtnToggleSidebarHarga_Click(object sender, RoutedEventArgs e)
        {
            var panel = FindName("pnlSidebarDetailHarga") as StackPanel;
            var image = FindName("pathToggleSidebarHarga") as Image;

            if (panel != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    // EXPAND
                    panel.Visibility = Visibility.Visible;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    // COLLAPSE
                    panel.Visibility = Visibility.Collapsed;
                    if (image != null && image.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }

                // Update semua harga setelah toggle untuk memastikan sinkronisasi
                if (_selectedSchedule != null)
                {
                    UpdateAllPriceDisplays(_selectedSchedule.Price);
                }
            }
        }

        private void ToggleDetailHarga(StackPanel panel, System.Windows.Shapes.Path path)
        {
            if (panel != null && path != null)
            {
                if (panel.Visibility == Visibility.Collapsed)
                {
                    panel.Visibility = Visibility.Visible;
                    if (path.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 180;
                    }
                }
                else
                {
                    panel.Visibility = Visibility.Collapsed;
                    if (path.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 0;
                    }
                }
            }
        }

        private async void BtnLanjutPembayaran_Click(object sender, RoutedEventArgs e)
        {
            // Validate Detail Pemesan
            if (IsPlaceholderText(txtNamaPemesan) || string.IsNullOrWhiteSpace(txtNamaPemesan.Text))
            {
                MessageBox.Show("Nama pemesan harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNamaPemesan.Focus();
                return;
            }

            if (IsPlaceholderText(txtNomorPonsel) || string.IsNullOrWhiteSpace(txtNomorPonsel.Text))
            {
                MessageBox.Show("Nomor ponsel harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNomorPonsel.Focus();
                return;
            }

            // Validasi nomor ponsel harus angka saja
            if (!System.Text.RegularExpressions.Regex.IsMatch(txtNomorPonsel.Text.Trim(), @"^[0-9]+$"))
            {
                MessageBox.Show("Nomor ponsel harus berupa angka saja!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNomorPonsel.Focus();
                return;
            }

            if (IsPlaceholderText(txtEmail) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Email harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            // Validasi format email harus mengandung @ dan .
            string email = txtEmail.Text.Trim();
            if (!email.Contains("@") || !email.Contains("."))
            {
                MessageBox.Show("Email harus mengandung @ dan . (titik)!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            // Validate Detail Penumpang (basic validation for at least one passenger)
            if (IsPlaceholderText(txtNamaPassenger1) || string.IsNullOrWhiteSpace(txtNamaPassenger1.Text))
            {
                MessageBox.Show("Data penumpang 1 harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                if (pnlPassenger1.Visibility == Visibility.Collapsed)
                {
                    pnlPassenger1.Visibility = Visibility.Visible;
                    if (pathTogglePassenger1.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 90;
                    }
                }
                txtNamaPassenger1.Focus();
                return;
            }

            if (IsPlaceholderText(txtIdPassenger1) || string.IsNullOrWhiteSpace(txtIdPassenger1.Text))
            {
                MessageBox.Show("Nomor identitas penumpang 1 harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                if (pnlPassenger1.Visibility == Visibility.Collapsed)
                {
                    pnlPassenger1.Visibility = Visibility.Visible;
                    if (pathTogglePassenger1.RenderTransform is RotateTransform rotate)
                    {
                        rotate.Angle = 90;
                    }
                }
                txtIdPassenger1.Focus();
                return;
            }

            // Validasi NIK penumpang 1 - Jika jenis identitas adalah KTP, harus 16 digit angka
            if (cmbIdentitas1?.SelectedItem is ComboBoxItem selectedIdentitas1)
            {
                string jenisIdentitas1 = selectedIdentitas1.Content.ToString() ?? "";
                if (jenisIdentitas1 == "KTP")
                {
                    string nik1 = txtIdPassenger1.Text.Trim();
                    if (!System.Text.RegularExpressions.Regex.IsMatch(nik1, @"^[0-9]{16}$"))
                    {
                        MessageBox.Show("NIK (KTP) harus berupa 16 digit angka!", "Validasi",
                            MessageBoxButton.OK, MessageBoxImage.Warning);

                        if (pnlPassenger1.Visibility == Visibility.Collapsed)
                        {
                            pnlPassenger1.Visibility = Visibility.Visible;
                            if (pathTogglePassenger1.RenderTransform is RotateTransform rotate)
                            {
                                rotate.Angle = 90;
                            }
                        }
                        txtIdPassenger1.Focus();
                        return;
                    }
                }
            }

            // Validate Detail Kendaraan - Only check if vehicle section is visible
            if (vehicleSection?.Visibility == Visibility.Visible &&
                (IsPlaceholderText(txtPlatNomor) || string.IsNullOrWhiteSpace(txtPlatNomor.Text)))
            {
                MessageBox.Show("Plat nomor kendaraan harus diisi!", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPlatNomor.Focus();
                return;
            }

            // ============ KODE BARU: SIMPAN KE DATABASE ============
            try
            {
                // Show loading
                btnLanjutPembayaran.IsEnabled = false;
                btnLanjutPembayaran.Content = "Memproses...";

                // Validasi session user
                if (TiketLaut.Services.SessionManager.CurrentUser == null)
                {
                    MessageBox.Show("Sesi login Anda telah berakhir. Silakan login kembali.", 
                        "Session Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi data schedule dan search criteria
                if (_selectedSchedule == null || _searchCriteria == null)
                {
                    MessageBox.Show("Data jadwal tidak lengkap. Silakan ulangi pemesanan dari awal.", 
                        "Data Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ambil data dari form
                var bookingData = new TiketLaut.Services.BookingData
                {
                    PenggunaId = TiketLaut.Services.SessionManager.CurrentUser.pengguna_id,
                    JadwalId = _selectedSchedule.JadwalId,
                    JenisKendaraanId = _searchCriteria.JenisKendaraanId,
                    JumlahPenumpang = _searchCriteria.JumlahPenumpang,
                    PlatNomor = vehicleSection?.Visibility == Visibility.Visible ? txtPlatNomor?.Text : null,
                    DataPenumpang = new List<TiketLaut.Services.PenumpangData>()
                };

                // Ambil data penumpang dari form (maksimal 3 penumpang sesuai UI)
                for (int i = 1; i <= Math.Min(_searchCriteria.JumlahPenumpang, 3); i++)
                {
                    var txtNama = FindName($"txtNamaPassenger{i}") as TextBox;
                    var txtId = FindName($"txtIdPassenger{i}") as TextBox;
                    var cmbIdentitas = FindName($"cmbIdentitas{i}") as ComboBox;
                    
                    // Get gender from RadioButtons
                    var rbTuan = FindName($"rbTuan{i}") as RadioButton;
                    var rbNyonya = FindName($"rbNyonya{i}") as RadioButton;
                    var rbNona = FindName($"rbNona{i}") as RadioButton;

                    if (txtNama != null && txtId != null && 
                        !string.IsNullOrWhiteSpace(txtNama.Text) && 
                        !IsPlaceholderText(txtNama) &&
                        !string.IsNullOrWhiteSpace(txtId.Text) &&
                        !IsPlaceholderText(txtId))
                    {
                        // Get jenis identitas first for validation
                        string jenisIdentitas = "KTP"; // Default
                        if (cmbIdentitas?.SelectedItem is ComboBoxItem selectedItem)
                        {
                            jenisIdentitas = selectedItem.Content.ToString() ?? "KTP";
                        }

                        // Parse nomor identitas
                        string nomorIdentitasText = txtId.Text.Trim();
                        if (!long.TryParse(nomorIdentitasText, out long nomorIdentitas))
                        {
                            MessageBox.Show($"Nomor identitas penumpang {i} harus berupa angka!", 
                                "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtId.Focus();
                            return;
                        }

                        // Validasi NIK jika jenis identitas adalah KTP
                        if (jenisIdentitas == "KTP")
                        {
                            if (!System.Text.RegularExpressions.Regex.IsMatch(nomorIdentitasText, @"^[0-9]{16}$"))
                            {
                                MessageBox.Show($"NIK (KTP) penumpang {i} harus berupa 16 digit angka!", 
                                    "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                                txtId.Focus();
                                return;
                            }
                        }

                        // Determine gender from RadioButton
                        string jenisKelamin = "Laki-laki"; // Default
                        if (rbTuan?.IsChecked == true)
                        {
                            jenisKelamin = "Laki-laki";
                        }
                        else if (rbNyonya?.IsChecked == true || rbNona?.IsChecked == true)
                        {
                            jenisKelamin = "Perempuan";
                        }

                        var penumpangData = new TiketLaut.Services.PenumpangData
                        {
                            Nama = txtNama.Text.Trim(),
                            NomorIdentitas = nomorIdentitas,
                            JenisIdentitas = jenisIdentitas,
                            JenisKelamin = jenisKelamin
                        };

                        bookingData.DataPenumpang.Add(penumpangData);
                        
                        System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] Penumpang {i}:");
                        System.Diagnostics.Debug.WriteLine($"  Nama: {penumpangData.Nama}");
                        System.Diagnostics.Debug.WriteLine($"  Jenis Kelamin: {penumpangData.JenisKelamin}");
                        System.Diagnostics.Debug.WriteLine($"  Jenis Identitas: {penumpangData.JenisIdentitas}");
                    }
                }

                // Ambil data penumpang tambahan dari form dinamis (penumpang 4+)
                var additionalContainer = FindName("additionalPassengerFormsContainer") as StackPanel;
                if (additionalContainer != null && _searchCriteria.JumlahPenumpang > 3)
                {
                    foreach (Border passengerBorder in additionalContainer.Children)
                    {
                        try
                        {
                            // Extract passenger number from the dynamic form
                            int passengerIndex = bookingData.DataPenumpang.Count + 1;
                            
                            // Find controls in dynamically generated form
                            var txtNama = FindChildControl<TextBox>(passengerBorder);
                            var cmbIdentitas = FindChildControl<ComboBox>(passengerBorder);
                            var radioButtons = FindChildControls<RadioButton>(passengerBorder);
                            
                            if (txtNama != null && !string.IsNullOrWhiteSpace(txtNama.Text))
                            {
                                // Get second TextBox (identity number) - skip the first one which is name
                                var allTextBoxes = FindChildControls<TextBox>(passengerBorder);
                                TextBox? txtId = allTextBoxes.Count > 1 ? allTextBoxes[1] : null;
                                
                                if (txtId != null && !string.IsNullOrWhiteSpace(txtId.Text))
                                {
                                    // Get jenis identitas first for validation
                                    string jenisIdentitas = "KTP";
                                    if (cmbIdentitas?.SelectedItem is ComboBoxItem selectedItem)
                                    {
                                        jenisIdentitas = selectedItem.Content.ToString() ?? "KTP";
                                    }

                                    string nomorIdentitasText = txtId.Text.Trim();
                                    if (long.TryParse(nomorIdentitasText, out long nomorIdentitas))
                                    {
                                        // Validasi NIK jika jenis identitas adalah KTP
                                        if (jenisIdentitas == "KTP")
                                        {
                                            if (!System.Text.RegularExpressions.Regex.IsMatch(nomorIdentitasText, @"^[0-9]{16}$"))
                                            {
                                                MessageBox.Show($"NIK (KTP) penumpang {passengerIndex} harus berupa 16 digit angka!", 
                                                    "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                                                
                                                // Reset and return - don't continue processing
                                                btnLanjutPembayaran.IsEnabled = true;
                                                btnLanjutPembayaran.Content = "Lanjut Pembayaran";
                                                return;
                                            }
                                        }

                                        // Determine gender from RadioButtons
                                        string jenisKelamin = "Laki-laki";
                                        foreach (var rb in radioButtons)
                                        {
                                            if (rb.IsChecked == true)
                                            {
                                                string content = rb.Content?.ToString() ?? "";
                                                if (content == "Nyonya" || content == "Nona")
                                                {
                                                    jenisKelamin = "Perempuan";
                                                }
                                                break;
                                            }
                                        }

                                        var penumpangData = new TiketLaut.Services.PenumpangData
                                        {
                                            Nama = txtNama.Text.Trim(),
                                            NomorIdentitas = nomorIdentitas,
                                            JenisIdentitas = jenisIdentitas,
                                            JenisKelamin = jenisKelamin
                                        };

                                        bookingData.DataPenumpang.Add(penumpangData);
                                        
                                        System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] Penumpang tambahan {passengerIndex}:");
                                        System.Diagnostics.Debug.WriteLine($"  Nama: {penumpangData.Nama}");
                                        System.Diagnostics.Debug.WriteLine($"  Jenis Kelamin: {penumpangData.JenisKelamin}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] Error reading dynamic passenger form: {ex.Message}");
                        }
                    }
                }

                // Validasi minimal ada 1 penumpang
                if (bookingData.DataPenumpang.Count == 0)
                {
                    MessageBox.Show("Data penumpang harus diisi minimal 1 orang!", 
                        "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi jumlah penumpang sesuai dengan yang dipilih
                if (bookingData.DataPenumpang.Count < _searchCriteria.JumlahPenumpang)
                {
                    MessageBox.Show(
                        $"Anda memilih {_searchCriteria.JumlahPenumpang} penumpang, tetapi hanya mengisi {bookingData.DataPenumpang.Count} data penumpang.\n\n" +
                        "Silakan lengkapi semua data penumpang!",
                        "Validasi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Simpan booking ke database
                var bookingService = new TiketLaut.Services.BookingService();
                var tiket = await bookingService.CreateBookingAsync(bookingData);

                MessageBox.Show(
                    $"? Booking berhasil!\n\n" +
                    $"Kode Tiket: {tiket.kode_tiket}\n" +
                    $"Total: Rp {tiket.total_harga:N0}\n\n" +
                    $"Silakan lanjutkan ke pembayaran.",
                    "Booking Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Navigasi ke PaymentWindow
                var paymentWindow = new PaymentWindow();
                paymentWindow.Left = this.Left;
                paymentWindow.Top = this.Top;
                paymentWindow.Width = this.Width;
                paymentWindow.Height = this.Height;
                paymentWindow.WindowState = this.WindowState;
                paymentWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"? Terjadi kesalahan saat memproses booking:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"[BookingDetailWindow] Error: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                btnLanjutPembayaran.IsEnabled = true;
                btnLanjutPembayaran.Content = "Lanjut Pembayaran";
            }
        }
    }
}

