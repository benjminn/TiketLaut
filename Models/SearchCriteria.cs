using System;

namespace TiketLaut.Models
{
    /// <summary>
    /// Data Transfer Object untuk kriteria pencarian jadwal
    /// Digunakan untuk passing data antara HomePage dan ScheduleWindow
    /// </summary>
    public class SearchCriteria
    {
        public int PelabuhanAsalId { get; set; }
        public int PelabuhanTujuanId { get; set; }
        public string KelasLayanan { get; set; } = "Reguler";
        public DateTime TanggalKeberangkatan { get; set; }
        public TimeOnly? JamKeberangkatan { get; set; }
        public int JumlahPenumpang { get; set; }
        public int JenisKendaraanId { get; set; }
    }
}