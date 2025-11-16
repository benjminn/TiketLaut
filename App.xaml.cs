using System;
using System.Diagnostics;
using System.Windows;
using TiketLaut.Services;

namespace TiketLaut
{
    public partial class App : Application
    {
        // ✅ Static instance untuk akses global
        private static NotifikasiBackgroundService _notifBackgroundService;

        public static NotifikasiBackgroundService NotifBackgroundService
        {
            get
            {
                if (_notifBackgroundService == null)
                {
                    _notifBackgroundService = new NotifikasiBackgroundService();
                }
                return _notifBackgroundService;
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Enable binding error logging
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            // ✅ CATCH-UP MISSED NOTIFICATIONS saat aplikasi dibuka
            try
            {
                var notifService = new NotifikasiService();
                await notifService.CatchUpMissedNotificationsAsync();
                Debug.WriteLine("[APP] 📧 Missed notifications catch-up completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[APP] ❌ Failed to catch up missed notifications: {ex.Message}");
            }

            // ✅ START BACKGROUND SERVICE
            NotifBackgroundService.Start(intervalMinutes: 1);

            Debug.WriteLine("[APP] ✅ Application started.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // ✅ STOP BACKGROUND SERVICE
            NotifBackgroundService.Stop();

            Debug.WriteLine("[APP] ❌ Application exiting.");
            Debug.WriteLine("[APP] 🔔 Notifikasi Background Service stopped.");

            base.OnExit(e);
        }
    }
}