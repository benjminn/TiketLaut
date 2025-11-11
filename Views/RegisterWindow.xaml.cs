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
using TiketLaut.Views.Components;

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
            cmbJenisKelamin.SelectedIndex = -1;
            dpTanggalLahir.SelectedDate = null;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
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
            btnRegister.IsEnabled = false;
            btnRegister.Content = "Memproses...";

            try
            {
                var (success, message) = await _penggunaService.RegisterAsync(pengguna);

                if (success)
                {
                    CustomDialog.ShowSuccess(
                        "Sukses",
                        "Registrasi berhasil!\n\nSilakan masuk dengan akun baru Anda.");

                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
                else
                {
                    CustomDialog.ShowWarning(
                        "Registrasi Gagal",
                        $"Registrasi gagal!\n\n{message}");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError(
                    "Error",
                    $"Terjadi kesalahan:\n\n{ex.Message}\n\nDetail: {ex.InnerException?.Message ?? "Tidak ada detail tambahan"}");
            }
            finally
            {
                btnRegister.IsEnabled = true;
                btnRegister.Content = "Daftar";
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtNamaLengkap.Text))
            {
                CustomDialog.ShowWarning("Peringatan", "Nama lengkap tidak boleh kosong!");
                txtNamaLengkap.Focus();
                return false;
            }
            if (cmbJenisKelamin.SelectedIndex == -1)
            {
                CustomDialog.ShowWarning("Peringatan", "Silakan pilih jenis kelamin!");
                cmbJenisKelamin.Focus();
                return false;
            }
            if (!dpTanggalLahir.SelectedDate.HasValue)
            {
                CustomDialog.ShowWarning("Peringatan", "Silakan pilih tanggal lahir!");
                dpTanggalLahir.Focus();
                return false;
            }
            if (dpTanggalLahir.SelectedDate.HasValue)
            {
                var age = DateTime.Now.Year - dpTanggalLahir.SelectedDate.Value.Year;
                if (dpTanggalLahir.SelectedDate.Value.AddYears(age) > DateTime.Now)
                    age--;

                if (age < 17)
                {
                    CustomDialog.ShowWarning("Peringatan", "Usia minimal untuk registrasi adalah 17 tahun!");
                    dpTanggalLahir.Focus();
                    return false;
                }
            }
            if (string.IsNullOrWhiteSpace(txtNIK.Text))
            {
                CustomDialog.ShowWarning("Peringatan", "NIK tidak boleh kosong!");
                txtNIK.Focus();
                return false;
            }

            if (txtNIK.Text.Length != 16 || !txtNIK.Text.All(char.IsDigit))
            {
                CustomDialog.ShowWarning("Peringatan", "NIK harus berupa 16 digit angka!");
                txtNIK.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                CustomDialog.ShowWarning("Peringatan", "Email tidak boleh kosong!");
                txtEmail.Focus();
                return false;
            }

            if (!IsValidEmail(txtEmail.Text))
            {
                CustomDialog.ShowWarning("Peringatan", "Format email tidak valid!");
                txtEmail.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(txtPassword.Password))
            {
                CustomDialog.ShowWarning("Peringatan", "Password tidak boleh kosong!");
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Password.Length < 8)
            {
                CustomDialog.ShowWarning("Peringatan", "Password minimal 8 karakter!");
                txtPassword.Focus();
                return false;
            }

            if (!txtPassword.Password.Any(char.IsLower) || !txtPassword.Password.Any(char.IsUpper))
            {
                CustomDialog.ShowWarning("Peringatan", "Password harus mengandung huruf kecil dan besar!");
                txtPassword.Focus();
                return false;
            }
            if (txtPassword.Password != txtKonfirmasiPassword.Password)
            {
                CustomDialog.ShowWarning("Peringatan", "Konfirmasi password tidak cocok!");
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
                    CustomDialog.ShowInfo(
                        "Info",
                        "Google OAuth belum dikonfigurasi!\n\nUntuk menggunakan real Google login:\n1. Buat project di Google Cloud Console\n2. Dapatkan Client ID & Secret\n3. Update di appsettings.json\n\nSementara akan menggunakan mode simulasi.");

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
                        CustomDialog.ShowWarning("Error", "Email tidak boleh kosong!");
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
                CustomDialog.ShowError("Error", $"Error Google OAuth: {ex.Message}");
            }
            finally
            {
                btnGoogleRegister.IsEnabled = true;
                btnGoogleRegister.Content = "Daftar dengan Google";
            }
        }
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
                        <!DOCTYPE html>
                        <html lang='id'>
                        <head>
                            <meta charset='UTF-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <title>Registrasi Gagal - TiketLaut</title>
                            <link href='https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap' rel='stylesheet'>
                            <style>
                                * {{
                                    margin: 0;
                                    padding: 0;
                                    box-sizing: border-box;
                                }}
                                
                                body {{
                                    font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                                    background: linear-gradient(135deg, #7F1D1D 0%, #991B1B 100%);
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    min-height: 100vh;
                                    padding: 20px;
                                }}
                                
                                .container {{
                                    background: white;
                                    border-radius: 24px;
                                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
                                    padding: 60px 50px;
                                    text-align: center;
                                    max-width: 480px;
                                    width: 100%;
                                    animation: slideUp 0.5s ease-out;
                                }}
                                
                                @keyframes slideUp {{
                                    from {{
                                        opacity: 0;
                                        transform: translateY(30px);
                                    }}
                                    to {{
                                        opacity: 1;
                                        transform: translateY(0);
                                    }}
                                }}
                                
                                .icon-container {{
                                    width: 100px;
                                    height: 100px;
                                    background: linear-gradient(135deg, #EF4444 0%, #DC2626 100%);
                                    border-radius: 50%;
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    margin: 0 auto 30px;
                                    animation: shake 0.5s ease-in-out;
                                }}
                                
                                @keyframes shake {{
                                    0%, 100% {{ transform: translateX(0); }}
                                    25% {{ transform: translateX(-10px); }}
                                    75% {{ transform: translateX(10px); }}
                                }}
                                
                                .error-icon {{
                                    color: white;
                                    font-size: 50px;
                                    font-weight: bold;
                                }}
                                
                                h1 {{
                                    color: #DC2626;
                                    font-size: 32px;
                                    font-weight: 700;
                                    margin-bottom: 16px;
                                    letter-spacing: -0.5px;
                                }}
                                
                                .subtitle {{
                                    color: #3E4958;
                                    font-size: 18px;
                                    font-weight: 500;
                                    margin-bottom: 12px;
                                }}
                                
                                .error-message {{
                                    background: #FEF2F2;
                                    border-left: 4px solid #EF4444;
                                    border-radius: 8px;
                                    padding: 18px 20px;
                                    margin: 25px 0;
                                    text-align: left;
                                }}
                                
                                .error-message p {{
                                    color: #991B1B;
                                    font-size: 14px;
                                    line-height: 1.5;
                                    margin: 0;
                                    word-break: break-word;
                                }}
                                
                                .error-message strong {{
                                    color: #7F1D1D;
                                    font-weight: 600;
                                }}
                                
                                .message {{
                                    color: #717182;
                                    font-size: 15px;
                                    line-height: 1.6;
                                }}
                                
                                .divider {{
                                    width: 60px;
                                    height: 4px;
                                    background: linear-gradient(90deg, #EF4444 0%, #DC2626 100%);
                                    border-radius: 2px;
                                    margin: 25px auto;
                                }}
                                
                                .btn-close {{
                                    display: inline-block;
                                    background: linear-gradient(135deg, #DC2626 0%, #991B1B 100%);
                                    color: white;
                                    padding: 14px 32px;
                                    border-radius: 12px;
                                    text-decoration: none;
                                    font-weight: 600;
                                    font-size: 15px;
                                    margin-top: 25px;
                                    transition: transform 0.2s, box-shadow 0.2s;
                                    cursor: pointer;
                                    border: none;
                                }}
                                
                                .btn-close:hover {{
                                    transform: translateY(-2px);
                                    box-shadow: 0 8px 20px rgba(220, 38, 38, 0.3);
                                }}
                                
                                .footer {{
                                    margin-top: 30px;
                                    padding-top: 25px;
                                    border-top: 1px solid #E5E7EB;
                                }}
                                
                                .footer p {{
                                    color: #9CA3AF;
                                    font-size: 13px;
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='icon-container'>
                                    <div class='error-icon'>✕</div>
                                </div>
                                
                                <h1>Registrasi Gagal</h1>
                                <div class='divider'></div>
                                
                                <p class='subtitle'>Terjadi kesalahan saat registrasi</p>
                                
                                <div class='error-message'>
                                    <p><strong>Error:</strong> {error}</p>
                                </div>
                                
                                <p class='message'>
                                    Silakan tutup jendela ini dan coba lagi dari aplikasi TiketLaut.
                                </p>
                                
                                <button class='btn-close' onclick='window.close()'>Tutup Jendela</button>
                                
                                <div class='footer'>
                                    <p>© 2025 TiketLaut - Sistem Pemesanan Tiket Kapal</p>
                                </div>
                            </div>
                        </body>
                        </html>";
                }
                else if (string.IsNullOrEmpty(code))
                {
                    responseHtml = @"
                        <!DOCTYPE html>
                        <html lang='id'>
                        <head>
                            <meta charset='UTF-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <title>Registrasi Gagal - TiketLaut</title>
                            <link href='https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap' rel='stylesheet'>
                            <style>
                                * {
                                    margin: 0;
                                    padding: 0;
                                    box-sizing: border-box;
                                }
                                
                                body {
                                    font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                                    background: linear-gradient(135deg, #7F1D1D 0%, #991B1B 100%);
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    min-height: 100vh;
                                    padding: 20px;
                                }
                                
                                .container {
                                    background: white;
                                    border-radius: 24px;
                                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
                                    padding: 60px 50px;
                                    text-align: center;
                                    max-width: 480px;
                                    width: 100%;
                                    animation: slideUp 0.5s ease-out;
                                }
                                
                                @keyframes slideUp {
                                    from {
                                        opacity: 0;
                                        transform: translateY(30px);
                                    }
                                    to {
                                        opacity: 1;
                                        transform: translateY(0);
                                    }
                                }
                                
                                .icon-container {
                                    width: 100px;
                                    height: 100px;
                                    background: linear-gradient(135deg, #EF4444 0%, #DC2626 100%);
                                    border-radius: 50%;
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    margin: 0 auto 30px;
                                    animation: shake 0.5s ease-in-out;
                                }
                                
                                @keyframes shake {
                                    0%, 100% { transform: translateX(0); }
                                    25% { transform: translateX(-10px); }
                                    75% { transform: translateX(10px); }
                                }
                                
                                .error-icon {
                                    color: white;
                                    font-size: 50px;
                                    font-weight: bold;
                                }
                                
                                h1 {
                                    color: #DC2626;
                                    font-size: 32px;
                                    font-weight: 700;
                                    margin-bottom: 16px;
                                    letter-spacing: -0.5px;
                                }
                                
                                .subtitle {
                                    color: #3E4958;
                                    font-size: 18px;
                                    font-weight: 500;
                                    margin-bottom: 12px;
                                }
                                
                                .error-message {
                                    background: #FEF2F2;
                                    border-left: 4px solid #EF4444;
                                    border-radius: 8px;
                                    padding: 18px 20px;
                                    margin: 25px 0;
                                    text-align: left;
                                }
                                
                                .error-message p {
                                    color: #991B1B;
                                    font-size: 14px;
                                    line-height: 1.5;
                                    margin: 0;
                                }
                                
                                .error-message strong {
                                    color: #7F1D1D;
                                    font-weight: 600;
                                }
                                
                                .message {
                                    color: #717182;
                                    font-size: 15px;
                                    line-height: 1.6;
                                }
                                
                                .divider {
                                    width: 60px;
                                    height: 4px;
                                    background: linear-gradient(90deg, #EF4444 0%, #DC2626 100%);
                                    border-radius: 2px;
                                    margin: 25px auto;
                                }
                                
                                .btn-close {
                                    display: inline-block;
                                    background: linear-gradient(135deg, #DC2626 0%, #991B1B 100%);
                                    color: white;
                                    padding: 14px 32px;
                                    border-radius: 12px;
                                    text-decoration: none;
                                    font-weight: 600;
                                    font-size: 15px;
                                    margin-top: 25px;
                                    transition: transform 0.2s, box-shadow 0.2s;
                                    cursor: pointer;
                                    border: none;
                                }
                                
                                .btn-close:hover {
                                    transform: translateY(-2px);
                                    box-shadow: 0 8px 20px rgba(220, 38, 38, 0.3);
                                }
                                
                                .footer {
                                    margin-top: 30px;
                                    padding-top: 25px;
                                    border-top: 1px solid #E5E7EB;
                                }
                                
                                .footer p {
                                    color: #9CA3AF;
                                    font-size: 13px;
                                }
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='icon-container'>
                                    <div class='error-icon'>✕</div>
                                </div>
                                
                                <h1>Registrasi Gagal</h1>
                                <div class='divider'></div>
                                
                                <p class='subtitle'>Kode otorisasi tidak ditemukan</p>
                                
                                <div class='error-message'>
                                    <p><strong>Error:</strong> Authorization code tidak ditemukan dalam respons OAuth.</p>
                                </div>
                                
                                <p class='message'>
                                    Silakan tutup jendela ini dan coba lagi dari aplikasi TiketLaut.
                                </p>
                                
                                <button class='btn-close' onclick='window.close()'>Tutup Jendela</button>
                                
                                <div class='footer'>
                                    <p>© 2025 TiketLaut - Sistem Pemesanan Tiket Kapal</p>
                                </div>
                            </div>
                        </body>
                        </html>";
                }
                else
                {
                    responseHtml = @"
                        <!DOCTYPE html>
                        <html lang='id'>
                        <head>
                            <meta charset='UTF-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <title>Registrasi Berhasil - TiketLaut</title>
                            <link href='https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap' rel='stylesheet'>
                            <style>
                                * {
                                    margin: 0;
                                    padding: 0;
                                    box-sizing: border-box;
                                }
                                
                                body {
                                    font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                                    background: linear-gradient(135deg, #042769 0%, #00658D 100%);
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    min-height: 100vh;
                                    padding: 20px;
                                }
                                
                                .container {
                                    background: white;
                                    border-radius: 24px;
                                    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
                                    padding: 60px 50px;
                                    text-align: center;
                                    max-width: 480px;
                                    width: 100%;
                                    animation: slideUp 0.5s ease-out;
                                }
                                
                                @keyframes slideUp {
                                    from {
                                        opacity: 0;
                                        transform: translateY(30px);
                                    }
                                    to {
                                        opacity: 1;
                                        transform: translateY(0);
                                    }
                                }
                                
                                .icon-container {
                                    width: 140px;
                                    height: 140px;
                                    background: linear-gradient(135deg, #D9A494 0%, #C89484 100%);
                                    border-radius: 50%;
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    margin: 0 auto 30px;
                                    animation: checkmark 0.6s ease-in-out 0.2s both;
                                    box-shadow: 0 8px 24px rgba(217, 164, 148, 0.35);
                                }
                                
                                @keyframes checkmark {
                                    0% {
                                        transform: scale(0);
                                        opacity: 0;
                                    }
                                    50% {
                                        transform: scale(1.1);
                                    }
                                    100% {
                                        transform: scale(1);
                                        opacity: 1;
                                    }
                                }
                                
                                .checkmark {
                                    width: 80px;
                                    height: 80px;
                                    stroke: white;
                                    stroke-width: 5;
                                    stroke-linecap: round;
                                    fill: none;
                                    animation: draw 0.5s ease-in-out 0.4s both;
                                }
                                
                                @keyframes draw {
                                    to {
                                        stroke-dashoffset: 0;
                                    }
                                }
                                
                                .checkmark {
                                    stroke-dasharray: 100;
                                    stroke-dashoffset: 100;
                                }
                                
                                h1 {
                                    color: #042769;
                                    font-size: 32px;
                                    font-weight: 700;
                                    margin-bottom: 16px;
                                    letter-spacing: -0.5px;
                                }
                                
                                .subtitle {
                                    color: #3E4958;
                                    font-size: 18px;
                                    font-weight: 500;
                                    margin-bottom: 12px;
                                }
                                
                                .message {
                                    color: #717182;
                                    font-size: 15px;
                                    line-height: 1.6;
                                    margin-bottom: 35px;
                                }
                                
                                .divider {
                                    width: 60px;
                                    height: 4px;
                                    background: linear-gradient(90deg, #D9A494 0%, #C89484 100%);
                                    border-radius: 2px;
                                    margin: 25px auto;
                                }
                                
                                .info-box {
                                    background: #F8F9FA;
                                    border-left: 4px solid #D9A494;
                                    border-radius: 8px;
                                    padding: 18px 20px;
                                    margin-top: 25px;
                                    text-align: left;
                                }
                                
                                .info-box p {
                                    color: #3E4958;
                                    font-size: 14px;
                                    line-height: 1.5;
                                    margin: 0;
                                }
                                
                                .info-box strong {
                                    color: #042769;
                                    font-weight: 600;
                                }
                                
                                .footer {
                                    margin-top: 30px;
                                    padding-top: 25px;
                                    border-top: 1px solid #E5E7EB;
                                }
                                
                                .footer p {
                                    color: #9CA3AF;
                                    font-size: 13px;
                                }
                                
                                .brand {
                                    color: #042769;
                                    font-weight: 700;
                                    font-size: 16px;
                                    display: inline-flex;
                                    align-items: center;
                                    gap: 6px;
                                }
                                
                                .wave {
                                    display: inline-block;
                                    animation: wave 0.5s ease-in-out 1s 3;
                                }
                                
                                @keyframes wave {
                                    0%, 100% { transform: rotate(0deg); }
                                    25% { transform: rotate(-10deg); }
                                    75% { transform: rotate(10deg); }
                                }
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='icon-container'>
                                    <svg class='checkmark' viewBox='0 0 52 52'>
                                        <path d='M14 27l7.5 7.5L38 18'/>
                                    </svg>
                                </div>
                                
                                <h1>Registrasi Berhasil!</h1>
                                <div class='divider'></div>
                                
                                <p class='subtitle'>Selamat datang!</p>
                                <p class='message'>
                                    Anda berhasil mendaftar menggunakan akun Google Anda.<br>
                                    Mohon klik 'OK' untuk kembali ke aplikasi TiketLaut.
                                </p>
                                
                                <div class='info-box'>
                                    <p><strong>📝 Langkah Selanjutnya:</strong><br> 
                                    Silakan lengkapi informasi profil Anda untuk melanjutkan menggunakan aplikasi</p>
                                </div>
                                
                                <div class='footer'>
                                    <p>© 2025 TiketLaut - Sistem Pemesanan Tiket Kapal</p>
                                </div>
                            </div>
                            
                            <script>
                                // Auto close after 2 seconds
                                setTimeout(function() {
                                    window.close();
                                }, 2000);
                            </script>
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

                    CustomDialog.ShowSuccess(
                        "Login Berhasil",
                        $"Selamat datang kembali, {existingUser.nama}!\n\nEmail {googleEmail} sudah terdaftar.\nAnda akan langsung masuk ke aplikasi.");
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

                            CustomDialog.ShowSuccess(
                                "Registrasi Berhasil",
                                $"Selamat datang, {pengguna.nama}!\n\nAkun Anda telah berhasil dibuat.");
                            var homePage = new HomePage(isLoggedIn: true, username: pengguna.nama);
                            homePage.Show();
                            this.Close();
                        }
                        else
                        {
                            CustomDialog.ShowError("Error", $"Registrasi gagal: {message}");
                        }
                    }
                    // Jika user cancel dialog, tidak perlu action (tetap di RegisterWindow)
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Error memproses Google register: {ex.Message}");
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