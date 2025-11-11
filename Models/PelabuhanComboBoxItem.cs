namespace TiketLaut.Models
{
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