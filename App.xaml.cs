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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Enable binding error logging
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            // ✅ START BACKGROUND SERVICE
            // Untuk testing: intervalMinutes: 1
            // Untuk production: intervalMinutes: 15
            NotifBackgroundService.Start(intervalMinutes: 1);

            Debug.WriteLine("[APP] ✅ Application started.");
            Debug.WriteLine($"[APP] 🔔 Notifikasi Background Service started.");
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