using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TiketLaut.Services
{
    /// <summary>
    /// Background service untuk auto-check notifikasi jadwal secara berkala
    /// </summary>
    public class NotifikasiBackgroundService
    {
        private readonly NotifikasiService _notifikasiService;
        private DispatcherTimer _timer;
        private bool _isRunning = false;

        public NotifikasiBackgroundService()
        {
            _notifikasiService = new NotifikasiService();
        }

        /// <summary>
        /// Mulai background service dengan interval pengecekan
        /// </summary>
        /// <param name="intervalMinutes">Interval pengecekan dalam menit (default: 15)</param>
        public void Start(int intervalMinutes = 15)
        {
            if (_isRunning)
            {
                System.Diagnostics.Debug.WriteLine("[NOTIF BG] Service sudah berjalan!");
                return;
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };

            _timer.Tick += async (s, e) => await CheckNotifikasiAsync();
            _timer.Start();
            _isRunning = true;

            System.Diagnostics.Debug.WriteLine($"[NOTIF BG] ✅ Service started. Interval: {intervalMinutes} menit");

            // Jalankan pengecekan pertama kali
            Task.Run(async () => await CheckNotifikasiAsync());
        }

        /// <summary>
        /// Stop background service
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
                _isRunning = false;
                System.Diagnostics.Debug.WriteLine("[NOTIF BG] ❌ Service stopped.");
            }
        }

        /// <summary>
        /// Pengecekan otomatis setiap interval
        /// </summary>
        private async Task CheckNotifikasiAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                System.Diagnostics.Debug.WriteLine($"[NOTIF BG] 🔄 Running auto-check at {now:yyyy-MM-dd HH:mm:ss} UTC");

                // Cek SEMUA tiket aktif (tanpa filter pengguna_id)
                await _notifikasiService.CekDanKirimNotifikasiJadwalOtomatisAsync();

                System.Diagnostics.Debug.WriteLine($"[NOTIF BG] ✅ Check completed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIF BG] ❌ Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NOTIF BG] Stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Force check manual (untuk testing atau admin trigger)
        /// </summary>
        public async Task ForceCheckAsync()
        {
            System.Diagnostics.Debug.WriteLine("[NOTIF BG] 🔧 Force check triggered manually.");
            await CheckNotifikasiAsync();
        }

        /// <summary>
        /// Cek apakah service sedang berjalan
        /// </summary>
        public bool IsRunning => _isRunning;
    }
}