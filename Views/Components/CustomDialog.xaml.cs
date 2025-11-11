using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TiketLaut.Views.Components
{
    public partial class CustomDialog : Window
    {
        public enum DialogType
        {
            Warning,
            Error,
            Success,
            Info,
            Question
        }

        public enum DialogButtons
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel
        }

        public new bool? DialogResult { get; private set; }

        public CustomDialog(string title, string message, DialogType type = DialogType.Info, DialogButtons buttons = DialogButtons.OK)
        {
            InitializeComponent();
            
            // Set window properties untuk modal behavior yang proper
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.ShowInTaskbar = false;
            this.ShowActivated = true;
            this.Focusable = true;
            this.Closing += (s, e) =>
            {
                // Only allow closing if DialogResult has been set (via button clicks)
                if (DialogResult == null)
                {
                    e.Cancel = true;
                }
            };
            
            // When dialog is loaded, temporarily set Topmost to ensure it appears on top
            // then immediately remove it so it stays with parent window layer
            this.Loaded += (s, e) =>
            {
                this.Topmost = true;
                this.Activate();
                this.Focus();
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Topmost = false;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            };
            
            txtTitle.Text = title;
            txtMessage.Text = message;
            
            // ConfigureDialogType(type); // No longer needed - no icon to configure
            ConfigureButtons(buttons);
        }

        // ConfigureDialogType method removed - no icon needed

        private void ConfigureButtons(DialogButtons buttons)
        {
            switch (buttons)
            {
                case DialogButtons.OK:
                    btnCancel.Visibility = Visibility.Collapsed;
                    btnConfirm.Content = "OK";
                    break;
                    
                case DialogButtons.OKCancel:
                    btnCancel.Content = "Batal";
                    btnConfirm.Content = "OK";
                    break;
                    
                case DialogButtons.YesNo:
                    btnCancel.Content = "Tidak";
                    btnConfirm.Content = "Ya";
                    break;
                    
                case DialogButtons.YesNoCancel:
                    btnCancel.Content = "Batal";
                    btnConfirm.Content = "Ya";
                    // Could add third button here if needed
                    break;
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Static helper methods untuk kemudahan penggunaan
        public static bool? Show(string title, string message, DialogType type = DialogType.Info, DialogButtons buttons = DialogButtons.OK)
        {
            var dialog = new CustomDialog(title, message, type, buttons);
            
            // Set owner to current active window untuk modal behavior
            bool ownerSet = false;
            try
            {
                if (Application.Current?.Windows != null && Application.Current.Windows.Count > 0)
                {
                    // Find the active window or main window that is shown and loaded
                    var potentialOwner = Application.Current.Windows.Cast<Window>()
                        .FirstOrDefault(w => w.IsActive && w.IsLoaded && w.IsVisible);
                    
                    if (potentialOwner == null)
                    {
                        // Fallback to main window if no active window
                        potentialOwner = Application.Current.MainWindow;
                    }
                    
                    // Only set owner if it's a valid, shown window and visible on screen
                    if (potentialOwner != null && potentialOwner.IsLoaded && potentialOwner.IsVisible && potentialOwner != dialog)
                    {
                        dialog.Owner = potentialOwner;
                        ownerSet = true;
                    }
                }
            }
            catch
            {
                // If owner setting fails, continue without owner (dialog will still work)
            }
            
            // If no owner could be set, use CenterScreen instead of CenterOwner
            if (!ownerSet)
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            
            dialog.ShowDialog();
            return dialog.DialogResult;
        }

        public static bool? ShowWarning(string title, string message, DialogButtons buttons = DialogButtons.OK)
        {
            return Show(title, message, DialogType.Warning, buttons);
        }

        public static bool? ShowError(string title, string message, DialogButtons buttons = DialogButtons.OK)
        {
            return Show(title, message, DialogType.Error, buttons);
        }

        public static bool? ShowSuccess(string title, string message, DialogButtons buttons = DialogButtons.OK)
        {
            return Show(title, message, DialogType.Success, buttons);
        }

        public static bool? ShowQuestion(string title, string message, DialogButtons buttons = DialogButtons.YesNo)
        {
            return Show(title, message, DialogType.Question, buttons);
        }

        public static bool? ShowInfo(string title, string message, DialogButtons buttons = DialogButtons.OK)
        {
            return Show(title, message, DialogType.Info, buttons);
        }
    }
}
