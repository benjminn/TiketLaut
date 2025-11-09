using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiketLaut.Services;
using TiketLaut.Views.Components;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;
using Newtonsoft.Json.Linq;

namespace TiketLaut.Views
{
    public enum LoginSource
    {
        HomePage,
        ScheduleWindow,
        Other
    }

    public partial class LoginWindow : Window
    {
        private readonly PenggunaService _penggunaService;
        private readonly AdminService _adminService;
        private readonly IConfiguration _configuration;
        private readonly LoginSource _loginSource; // ✅ FIXED: Added missing field

        // Google OAuth Config - Dibaca dari Environment Variables atau appsettings.json
        private readonly string GOOGLE_CLIENT_ID;
        private readonly string GOOGLE_CLIENT_SECRET;
        private readonly string REDIRECT_URI;
        private readonly int REDIRECT_PORT;

        // Constructor default (untuk backward compatibility)
        public LoginWindow() : this(LoginSource.HomePage)
        {
        }

        public LoginWindow(LoginSource source)
        {
            InitializeComponent();
            _loginSource = source; // ✅ Now this will work

            System.Diagnostics.Debug.WriteLine($"[LoginWindow] Constructor called with source: {source}");

            _penggunaService = new PenggunaService();
            _adminService = new AdminService();

            // ✅ FIXED: Changed 'configuration' to '_configuration'
            // Load configuration dari appsettings.json
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Baca Google OAuth config dengan prioritas:
            // 1. Environment Variables (production)
            // 2. appsettings.json (development)
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

            // Debug log untuk verifikasi config
            Debug.WriteLine($"[Config] Google Client ID: {(GOOGLE_CLIENT_ID != "NOT_CONFIGURED" ? "Configured" : "Not Configured")}");
            Debug.WriteLine($"[Config] Redirect URI: {REDIRECT_URI}");

            // Test database connection on load
            TestDatabaseConnection();
        }

        private async void TestDatabaseConnection()
        {
            try
            {
                var isConnected = await DatabaseService.TestConnectionAsync();
                if (!isConnected)
                {
                    CustomDialog.ShowError(
                        "Database Error",
                        "⚠️ Tidak dapat terhubung ke database Supabase!\n\nPastikan:\n1. Koneksi internet aktif\n2. Connection string di appsettings.json benar\n3. Database Supabase sudah dibuat");
                }
                else
                {
                    // Optional: tampilkan pesan sukses (comment jika tidak perlu)
                    // CustomDialog.ShowSuccess("Success", "✅ Koneksi ke database berhasil!");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Error testing connection: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler untuk tombol kembali - redirect berdasarkan source
        /// </summary>
        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LoginWindow] BtnKembali_Click - Source: {_loginSource}"); // ✅ Debug log

                switch (_loginSource)
                {
                    case LoginSource.HomePage:
                        System.Diagnostics.Debug.WriteLine("[LoginWindow] Navigating back to HomePage");
                        // Kembali ke HomePage sebagai guest
                        var homePage = new HomePage(isLoggedIn: false, username: "");
                        CopyWindowProperties(homePage);
                        homePage.Show();
                        this.Close();
                        break;

                    case LoginSource.ScheduleWindow:
                        System.Diagnostics.Debug.WriteLine("[LoginWindow] Navigating back to ScheduleWindow");
                        // Kembali ke ScheduleWindow dengan data session yang tersimpan
                        var scheduleWindow = new ScheduleWindow(); // Constructor akan auto-load dari session
                        CopyWindowProperties(scheduleWindow);
                        scheduleWindow.Show();
                        this.Close();
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine("[LoginWindow] Using default fallback to HomePage");
                        // Fallback ke HomePage
                        var defaultHomePage = new HomePage(isLoggedIn: false, username: "");
                        CopyWindowProperties(defaultHomePage);
                        defaultHomePage.Show();
                        this.Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Terjadi kesalahan saat kembali:\n\n{ex.Message}");
            }
        }

        /// <summary>
        /// Helper method untuk copy window properties
        /// </summary>
        private void CopyWindowProperties(Window targetWindow)
        {
            targetWindow.Left = this.Left;
            targetWindow.Top = this.Top;
            targetWindow.Width = this.Width;
            targetWindow.Height = this.Height;
            targetWindow.WindowState = this.WindowState;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            // Validasi input
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                CustomDialog.ShowWarning("Login Gagal", "Email dan password tidak boleh kosong!");
                return;
            }

            // Show loading
            btnLogin.IsEnabled = false;
            btnLogin.Content = "Memproses...";

            try
            {
                // Cek dulu apakah admin
                var admin = await _adminService.ValidateAdminLoginAsync(email, password);

                if (admin != null)
                {
                    // Login sebagai Admin - redirect ke Admin Dashboard
                    SessionManager.CurrentAdmin = admin;

                    CustomDialog.ShowSuccess("Login Berhasil", $"Selamat datang, Admin {admin.nama}!");

                    // Buka Admin Dashboard
                    var adminDashboard = new AdminDashboard();
                    adminDashboard.Show();
                    this.Close();
                    return;
                }

                // Jika bukan admin, cek sebagai pengguna biasa
                var pengguna = await _penggunaService.ValidateLoginAsync(email, password);

                if (pengguna != null)
                {
                    // Login berhasil - simpan session
                    SessionManager.CurrentUser = pengguna;

                    CustomDialog.ShowSuccess("Login Berhasil", $"Selamat datang, {pengguna.nama}!");

                    // ✅ UPDATED: Navigate berdasarkan source
                    NavigateAfterSuccessfulLogin(pengguna);
                }
                else
                {
                    CustomDialog.ShowWarning("Login Gagal", "Email atau password salah!");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Terjadi kesalahan:\n\n{ex.Message}");
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Masuk";
            }
        }

        /// <summary>
        /// Navigate setelah login berhasil berdasarkan source
        /// </summary>
        private void NavigateAfterSuccessfulLogin(Pengguna pengguna)
        {
            try
            {
                switch (_loginSource)
                {
                    case LoginSource.ScheduleWindow:
                        // Kembali ke ScheduleWindow dengan data session dan user sudah login
                        var scheduleWindow = new ScheduleWindow();
                        CopyWindowProperties(scheduleWindow);
                        scheduleWindow.Show();
                        this.Close();
                        break;

                    case LoginSource.HomePage:
                    default:
                        // Default ke HomePage dengan login state
                        var homePage = new HomePage(isLoggedIn: true, username: pengguna.nama);
                        CopyWindowProperties(homePage);
                        homePage.Show();
                        this.Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Fallback ke HomePage jika ada error
                var homePage = new HomePage(isLoggedIn: true, username: pengguna.nama);
                CopyWindowProperties(homePage);
                homePage.Show();
                this.Close();

                Debug.WriteLine($"[LoginWindow] Error in NavigateAfterSuccessfulLogin: {ex.Message}");
            }
        }

        private async void BtnGoogle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnGoogle.IsEnabled = false;
                btnGoogle.Content = "Menghubungkan...";

                // Cek apakah Client ID dan Secret sudah diisi
                if (GOOGLE_CLIENT_ID.Contains("YOUR_CLIENT_ID") || GOOGLE_CLIENT_SECRET.Contains("YOUR_CLIENT_SECRET"))
                {
                    // Fallback ke mode simulasi jika credentials belum diisi
                    CustomDialog.ShowInfo(
                        "Info",
                        "Google OAuth belum dikonfigurasi!\n\nUntuk menggunakan real Google login:\n1. Buat project di Google Cloud Console\n2. Dapatkan Client ID & Secret\n3. Update GOOGLE_CLIENT_ID dan GOOGLE_CLIENT_SECRET di kode\n\nSementara akan menggunakan mode simulasi.");

                    // Mode simulasi
                    var inputDialog = new GoogleEmailInputDialog();
                    bool? dialogResult = inputDialog.ShowDialog();

                    if (dialogResult != true)
                    {
                        btnGoogle.IsEnabled = true;
                        btnGoogle.Content = "Google";
                        return;
                    }

                    string googleEmail = inputDialog.GoogleEmail;
                    string googleName = inputDialog.GoogleName;

                    if (string.IsNullOrWhiteSpace(googleEmail))
                    {
                        CustomDialog.ShowWarning("Error", "Email tidak boleh kosong!");
                        btnGoogle.IsEnabled = true;
                        btnGoogle.Content = "Google";
                        return;
                    }

                    await ProcessGoogleLoginAsync(googleEmail, googleName);
                }
                else
                {
                    // REAL GOOGLE OAUTH FLOW
                    await PerformRealGoogleOAuthAsync();
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Error Google OAuth: {ex.Message}\n\n{ex.StackTrace}");
            }
            finally
            {
                btnGoogle.IsEnabled = true;
                btnGoogle.Content = "Google";
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
                string codeChallenge = GenerateCodeChallenge();

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
                string? returnedState = queryParams["state"];
                string? error = queryParams["error"];

                // 6. Send response HTML ke browser
                string responseHtml;
                if (!string.IsNullOrEmpty(error))
                {
                    responseHtml = $@"
                        <html>
                        <head><title>Login Gagal</title></head>
                        <body style='font-family: Arial; text-align: center; padding: 50px;'>
                            <h1 style='color: #d32f2f;'>Login Gagal</h1>
                            <p>Error: {error}</p>
                            <p>Anda bisa menutup halaman ini dan kembali ke aplikasi.</p>
                        </body>
                        </html>";
                }
                else if (string.IsNullOrEmpty(code))
                {
                    responseHtml = @"
                        <html>
                        <head><title>Login Gagal</title></head>
                        <body style='font-family: Arial; text-align: center; padding: 50px;'>
                            <h1 style='color: #d32f2f;'>Login Gagal</h1>
                            <p>Authorization code tidak ditemukan.</p>
                            <p>Anda bisa menutup halaman ini dan kembali ke aplikasi.</p>
                        </body>
                        </html>";
                }
                else
                {
                    responseHtml = @"
                        <html>
                        <head><title>Login Berhasil</title></head>
                        <body style='font-family: Arial; text-align: center; padding: 50px;'>
                            <h1 style='color: #4CAF50;'>✓ Login Berhasil!</h1>
                            <p>Anda berhasil login dengan Google.</p>
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

                // 10. Process login dengan data dari Google
                await ProcessGoogleLoginAsync(userInfo.email, userInfo.name);
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

            // Parse JSON response (simple parsing untuk demo)
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

            // Parse JSON response
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

        private string GenerateCodeChallenge()
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

        private async Task ProcessGoogleLoginAsync(string googleEmail, string googleName)
        {
            try
            {
                // Cek apakah user dengan Google email sudah terdaftar
                var existingUser = await _penggunaService.GetByGoogleEmailAsync(googleEmail);

                if (existingUser != null)
                {
                    // User sudah terdaftar - langsung login
                    SessionManager.CurrentUser = existingUser;

                    CustomDialog.ShowSuccess("Login Berhasil", $"Selamat datang kembali, {existingUser.nama}!");

                    // ✅ UPDATED: Navigate berdasarkan source
                    NavigateAfterSuccessfulLogin(existingUser);
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

                            CustomDialog.ShowSuccess("Registrasi Berhasil", $"Selamat datang, {pengguna.nama}!\n\nAkun Anda telah berhasil dibuat.");

                            // ✅ UPDATED: Navigate berdasarkan source
                            NavigateAfterSuccessfulLogin(pengguna);
                        }
                        else
                        {
                            CustomDialog.ShowError("Error", $"Registrasi gagal: {message}");
                        }
                    }
                    // Jika user cancel dialog, tidak perlu action (tetap di LoginWindow)
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Error memproses Google login: {ex.Message}");
            }
        }

        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void txtEmail_TextChanged_1(object sender, TextChangedEventArgs e)
        {
        }

        private void TxtBuatAkun_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}
