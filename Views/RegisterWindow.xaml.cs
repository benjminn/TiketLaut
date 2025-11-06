using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Configuration;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly PenggunaService _penggunaService;
        private readonly AdminService _adminService;
        private readonly IConfiguration _configuration;
        
        // Google OAuth Config
        private readonly string GOOGLE_CLIENT_ID;
        private readonly string GOOGLE_CLIENT_SECRET;
        private readonly string REDIRECT_URI;
        private readonly int REDIRECT_PORT;

        public RegisterWindow()
        {
            InitializeComponent();
            _penggunaService = new PenggunaService();
            _adminService = new AdminService();
            
            // Load configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            // Read Google OAuth config
            GOOGLE_CLIENT_ID = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
                ?? _configuration["GoogleOAuth:ClientId"] 
                ?? "NOT_CONFIGURED";
            
            GOOGLE_CLIENT_SECRET = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") 
                ?? _configuration["GoogleOAuth:ClientSecret"] 
                ?? "NOT_CONFIGURED";
            
            REDIRECT_URI = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI") 
                ?? _configuration["GoogleOAuth:RedirectUri"] 
                ?? "http://localhost:8080/";
            
            REDIRECT_PORT = int.TryParse(
                Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_PORT") 
                ?? _configuration["GoogleOAuth:RedirectPort"], 
                out int port) ? port : 8080;

            // Set default values
            cmbJenisKelamin.SelectedIndex = -1;
            dpTanggalLahir.SelectedDate = null;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Validasi input
            if (!ValidateInput())
                return;

            // Ambil data dari form
            var pengguna = new Pengguna
            {
                nama = txtNamaLengkap.Text.Trim(),
                jenis_kelamin = (cmbJenisKelamin.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "",
                tanggal_lahir = DateOnly.FromDateTime(dpTanggalLahir.SelectedDate!.Value),
                email = txtEmail.Text.Trim(),
                password = txtPassword.Password,
                nomor_induk_kependudukan = txtNIK.Text.Trim(),  // CHANGED: from "no_hp" to "nomor_induk_kependudukan"
                kewarganegaraan = "Indonesia",
                alamat = null,
                tanggal_daftar = DateTime.UtcNow
            };

            // Show loading
            btnRegister.IsEnabled = false;
            btnRegister.Content = "Memproses...";

            try
            {
                var (success, message) = await _penggunaService.RegisterAsync(pengguna);

                if (success)
                {
                    MessageBox.Show(
                        "? Registrasi berhasil!\n\nSilakan login dengan akun baru Anda.",
                        "Sukses",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        $"? Registrasi gagal!\n\n{message}",
                        "Registrasi Gagal",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"?? Terjadi kesalahan:\n\n{ex.Message}\n\n" +
                    $"Detail: {ex.InnerException?.Message ?? "Tidak ada detail tambahan"}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btnRegister.IsEnabled = true;
                btnRegister.Content = "Daftar";
            }
        }

        private bool ValidateInput()
        {
            // Validasi Nama Lengkap
            if (string.IsNullOrWhiteSpace(txtNamaLengkap.Text))
            {
                MessageBox.Show("Nama lengkap tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNamaLengkap.Focus();
                return false;
            }

            // Validasi Jenis Kelamin
            if (cmbJenisKelamin.SelectedIndex == -1)
            {
                MessageBox.Show("Silakan pilih jenis kelamin!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbJenisKelamin.Focus();
                return false;
            }

            // Validasi Tanggal Lahir
            if (!dpTanggalLahir.SelectedDate.HasValue)
            {
                MessageBox.Show("Silakan pilih tanggal lahir!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpTanggalLahir.Focus();
                return false;
            }

            // Validasi umur minimal 17 tahun
            if (dpTanggalLahir.SelectedDate.HasValue)
            {
                var age = DateTime.Now.Year - dpTanggalLahir.SelectedDate.Value.Year;
                if (dpTanggalLahir.SelectedDate.Value.AddYears(age) > DateTime.Now)
                    age--;

                if (age < 17)
                {
                    MessageBox.Show("Usia minimal untuk registrasi adalah 17 tahun!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    dpTanggalLahir.Focus();
                    return false;
                }
            }

            // Validasi NIK
            if (string.IsNullOrWhiteSpace(txtNIK.Text))
            {
                MessageBox.Show("NIK tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNIK.Focus();
                return false;
            }

            if (txtNIK.Text.Length != 16 || !txtNIK.Text.All(char.IsDigit))
            {
                MessageBox.Show("NIK harus berupa 16 digit angka!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNIK.Focus();
                return false;
            }

            // Validasi Email
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Email tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            if (!IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Format email tidak valid!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            // Validasi Password
            if (string.IsNullOrEmpty(txtPassword.Password))
            {
                MessageBox.Show("Password tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Password.Length < 8)
            {
                MessageBox.Show("Password minimal 8 karakter!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (!txtPassword.Password.Any(char.IsLower) || !txtPassword.Password.Any(char.IsUpper))
            {
                MessageBox.Show("Password harus mengandung huruf kecil dan besar!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            // Validasi Konfirmasi Password
            if (txtPassword.Password != txtKonfirmasiPassword.Password)
            {
                MessageBox.Show("Konfirmasi password tidak cocok!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtKonfirmasiPassword.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async void BtnGoogleRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnGoogleRegister.IsEnabled = false;
                btnGoogleRegister.Content = "Menghubungkan...";

                // Cek apakah Client ID dan Secret sudah diisi
                if (GOOGLE_CLIENT_ID.Contains("NOT_CONFIGURED") || GOOGLE_CLIENT_SECRET.Contains("NOT_CONFIGURED"))
                {
                    // Fallback ke mode simulasi jika credentials belum diisi
                    MessageBox.Show(
                        "Google OAuth belum dikonfigurasi!\n\n" +
                        "Untuk menggunakan real Google login:\n" +
                        "1. Buat project di Google Cloud Console\n" +
                        "2. Dapatkan Client ID & Secret\n" +
                        "3. Update di appsettings.json\n\n" +
                        "Sementara akan menggunakan mode simulasi.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Mode simulasi
                    var inputDialog = new GoogleEmailInputDialog();
                    bool? dialogResult = inputDialog.ShowDialog();

                    if (dialogResult != true)
                    {
                        btnGoogleRegister.IsEnabled = true;
                        btnGoogleRegister.Content = "Daftar dengan Google";
                        return;
                    }

                    string googleEmail = inputDialog.GoogleEmail;
                    string googleName = inputDialog.GoogleName;

                    if (string.IsNullOrWhiteSpace(googleEmail))
                    {
                        MessageBox.Show("Email tidak boleh kosong!",
                                       "Error",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Warning);
                        btnGoogleRegister.IsEnabled = true;
                        btnGoogleRegister.Content = "Daftar dengan Google";
                        return;
                    }

                    await ProcessGoogleRegisterAsync(googleEmail, googleName);
                }
                else
                {
                    // REAL GOOGLE OAUTH FLOW
                    await PerformRealGoogleOAuthAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error Google OAuth: {ex.Message}",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
            finally
            {
                btnGoogleRegister.IsEnabled = true;
                btnGoogleRegister.Content = "Daftar dengan Google";
            }
        }

        /// <summary>
        /// Real Google OAuth flow - membuka browser untuk login
        /// </summary>
        private async Task PerformRealGoogleOAuthAsync()
        {
            HttpListener? listener = null;
            
            try
            {
                // 1. Setup local HTTP listener untuk menerima OAuth callback
                listener = new HttpListener();
                listener.Prefixes.Add(REDIRECT_URI);
                listener.Start();
                
                Debug.WriteLine($"[OAuth] Listening on {REDIRECT_URI}");

                // 2. Generate authorization URL
                string state = GenerateRandomState();
                
                string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={GOOGLE_CLIENT_ID}" +
                    $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
                    $"&response_type=code" +
                    $"&scope={Uri.EscapeDataString("openid email profile")}" +
                    $"&state={state}";

                Debug.WriteLine($"[OAuth] Opening browser: {authUrl}");

                // 3. Buka browser default untuk login Google
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                // 4. Tunggu callback dari Google
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // 5. Parse authorization code dari callback URL
                var queryParams = request.QueryString;
                string? code = queryParams["code"];
                string? error = queryParams["error"];

                // 6. Send response HTML ke browser
                string responseHtml;
                if (!string.IsNullOrEmpty(error))
                {
                    responseHtml = $@"
                        <html>
                        <head><title>Registrasi Gagal</title></head>
                        <body style='font-family: Arial; text-align: center; padding: 50px;'>
                            <h1 style='color: #d32f2f;'>Registrasi Gagal</h1>
                            <p>Error: {error}</p>
                            <p>Anda bisa menutup halaman ini dan kembali ke aplikasi.</p>
                        </body>
                        </html>";
                }
                else if (string.IsNullOrEmpty(code))
                {
                    responseHtml = @"
                        <html>
                        <head><title>Registrasi Gagal</title></head>
                        <body style='font-family: Arial; text-align: center; padding: 50px;'>
                            <h1 style='color: #d32f2f;'>Registrasi Gagal</h1>
                            <p>Authorization code tidak ditemukan.</p>
                            <p>Anda bisa menutup halaman ini dan kembali ke aplikasi.</p>
                        </body>
                        </html>";
                }
                else
                {
                    responseHtml = @"
                        <html>
                        <head><title>Registrasi Berhasil</title></head>
                        <body style='font-family: Arial; text-align: center; padding: 50px;'>
                            <h1 style='color: #4CAF50;'>✓ Registrasi Berhasil!</h1>
                            <p>Anda berhasil mendaftar dengan Google.</p>
                            <p>Silakan kembali ke aplikasi TiketLaut.</p>
                            <script>window.close();</script>
                        </body>
                        </html>";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html; charset=utf-8";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                // 7. Jika ada error, hentikan proses
                if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                {
                    throw new Exception($"Google OAuth error: {error ?? "No authorization code received"}");
                }

                // 8. Exchange authorization code untuk access token
                var tokenResponse = await ExchangeCodeForTokenAsync(code);
                
                // 9. Gunakan access token untuk mendapatkan user info
                var userInfo = await GetGoogleUserInfoAsync(tokenResponse.access_token);
                
                Debug.WriteLine($"[OAuth] User info: {userInfo.email}, {userInfo.name}");

                // 10. Process register dengan data dari Google
                await ProcessGoogleRegisterAsync(userInfo.email, userInfo.name);
            }
            catch (HttpListenerException httpEx)
            {
                Debug.WriteLine($"[OAuth] HttpListener error: {httpEx.Message}");
                throw new Exception($"Error starting local server: {httpEx.Message}\n\n" +
                    "Pastikan port 8080 tidak digunakan aplikasi lain.");
            }
            finally
            {
                listener?.Stop();
                listener?.Close();
            }
        }

        /// <summary>
        /// Exchange authorization code untuk access token
        /// </summary>
        private async Task<GoogleTokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            using var httpClient = new HttpClient();
            
            var parameters = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", GOOGLE_CLIENT_ID },
                { "client_secret", GOOGLE_CLIENT_SECRET },
                { "redirect_uri", REDIRECT_URI },
                { "grant_type", "authorization_code" }
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to exchange code for token: {responseContent}");
            }

            var json = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = json.RootElement;

            return new GoogleTokenResponse
            {
                access_token = root.GetProperty("access_token").GetString() ?? "",
                token_type = root.GetProperty("token_type").GetString() ?? "Bearer",
                expires_in = root.GetProperty("expires_in").GetInt32(),
                id_token = root.TryGetProperty("id_token", out var idToken) ? idToken.GetString() : null
            };
        }

        /// <summary>
        /// Dapatkan user info dari Google menggunakan access token
        /// </summary>
        private async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get user info: {responseContent}");
            }

            var json = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = json.RootElement;

            return new GoogleUserInfo
            {
                id = root.GetProperty("id").GetString() ?? "",
                email = root.GetProperty("email").GetString() ?? "",
                name = root.GetProperty("name").GetString() ?? "",
                picture = root.TryGetProperty("picture", out var pic) ? pic.GetString() : null,
                verified_email = root.TryGetProperty("verified_email", out var verified) && verified.GetBoolean()
            };
        }

        private string GenerateRandomState()
        {
            var random = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(random);
            }
            return Convert.ToBase64String(random).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        // Helper classes untuk parse Google API responses
        private class GoogleTokenResponse
        {
            public string access_token { get; set; } = "";
            public string token_type { get; set; } = "";
            public int expires_in { get; set; }
            public string? id_token { get; set; }
        }

        private class GoogleUserInfo
        {
            public string id { get; set; } = "";
            public string email { get; set; } = "";
            public string name { get; set; } = "";
            public string? picture { get; set; }
            public bool verified_email { get; set; }
        }

        /// <summary>
        /// Process Google Register - sama seperti login, cek email dulu
        /// Jika sudah terdaftar, langsung login. Jika belum, register baru
        /// </summary>
        private async Task ProcessGoogleRegisterAsync(string googleEmail, string googleName)
        {
            try
            {
                // Cek apakah user dengan Google email sudah terdaftar
                var existingUser = await _penggunaService.GetByGoogleEmailAsync(googleEmail);

                if (existingUser != null)
                {
                    // User sudah terdaftar - langsung login (redirect ke home)
                    SessionManager.CurrentUser = existingUser;

                    MessageBox.Show($"Email {googleEmail} sudah terdaftar.\nAnda akan langsung masuk.",
                                   "Akun Sudah Ada",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    // Buka HomePage
                    var homePage = new HomePage(isLoggedIn: true, username: existingUser.nama);
                    homePage.Show();
                    this.Close();
                }
                else
                {
                    // User baru - buka dialog untuk lengkapi profil
                    var completeWindow = new GoogleOAuthCompleteWindow(googleEmail, googleName);
                    bool? dialogResult = completeWindow.ShowDialog();

                    if (dialogResult == true && completeWindow.IsCompleted)
                    {
                        // Register user baru dengan info lengkap
                        var (success, message, pengguna) = await _penggunaService.RegisterGoogleUserAsync(
                            googleEmail,
                            completeWindow.NamaLengkap,
                            completeWindow.NIK,
                            completeWindow.TanggalLahir);

                        if (success && pengguna != null)
                        {
                            // Set session
                            SessionManager.CurrentUser = pengguna;

                            MessageBox.Show($"Selamat datang, {pengguna.nama}!\n\nAkun Anda telah berhasil dibuat.",
                                           "Registrasi Berhasil",
                                           MessageBoxButton.OK,
                                           MessageBoxImage.Information);

                            // Buka HomePage
                            var homePage = new HomePage(isLoggedIn: true, username: pengguna.nama);
                            homePage.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show($"Registrasi gagal: {message}",
                                           "Error",
                                           MessageBoxButton.OK,
                                           MessageBoxImage.Error);
                        }
                    }
                    // Jika user cancel dialog, tidak perlu action (tetap di RegisterWindow)
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error memproses Google register: {ex.Message}",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        private void TxtMasuk_Click(object sender, MouseButtonEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void txtNamaLengkap_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}