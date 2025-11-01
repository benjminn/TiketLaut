namespace TiketLaut.Models
{
    /// <summary>
    /// Model helper untuk item ComboBox Pelabuhan
    /// Memisahkan ID dan Display Text untuk binding
    /// </summary>
    public class PelabuhanComboBoxItem
    {
        public int Id { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayText;
        }
    }
}