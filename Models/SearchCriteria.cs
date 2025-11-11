using System;

namespace TiketLaut.Models
{
    public class SearchCriteria
    {
        public int PelabuhanAsalId { get; set; }
        public int PelabuhanTujuanId { get; set; }
        public string KelasLayanan { get; set; } = "Reguler";
        public DateTime TanggalKeberangkatan { get; set; }
        public int? JamKeberangkatan { get; set; } // Jam 0-23, null jika tidak dipilih
        public int JumlahPenumpang { get; set; }
        public int JenisKendaraanId { get; set; }
    }
}