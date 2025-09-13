using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Admin
    {
        public int admin_id { get; set; }
        public string nama { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;

        public bool login()
        {
            // Implementasi login admin
            return true;
        }

        public void kelolaPembayaran()
        {
            // Implementasi kelola pembayaran
        }

        public void kelolaTiket()
        {
            // Implementasi kelola tiket
        }

        public void kelolaJadwal()
        {
            // Implementasi kelola jadwal
        }

        public void kelolaPengguna()
        {
            // Implementasi kelola pengguna
        }

        public void kelolaKapal()
        {
            // Implementasi kelola kapal
        }

        public void kelolaPelabuhan()
        {
            // Implementasi kelola pelabuhan
        }

        public void kirimNotifikasiBroadcast(string pesan, JenisNotifikasi jenis, Jadwal? jadwal = null)
        {
            // Implementasi kirim notifikasi broadcast ke semua pengguna
            var notifikasi = new Notifikasi
            {
                admin = this,
                admin_id = this.admin_id,
                jenis_enum_penumpang_update_status = jenis,
                pesan = pesan,
                is_broadcast = true,
                jadwal = jadwal,
                jadwal_id = jadwal?.jadwal_id
            };
            notifikasi.kirimBroadcastNotifikasi();
        }

        public void kirimNotifikasiPerubahanJadwal(Jadwal jadwal, string alasanPerubahan)
        {
            // Implementasi khusus untuk notifikasi perubahan jadwal
            string pesanNotifikasi = $"PEMBERITAHUAN PERUBAHAN JADWAL: " +
                $"Jadwal keberangkatan dari Pelabuhan ID {jadwal.pelabuhan_asal_id} " +
                $"ke Pelabuhan ID {jadwal.pelabuhan_tujuan_id} " +
                $"pada {jadwal.tanggal_berangkat:dd/MM/yyyy} jam {jadwal.waktu_berangkat} " +
                $"mengalami perubahan. Alasan: {alasanPerubahan}. " +
                $"Mohon cek aplikasi untuk update terbaru.";
            kirimNotifikasiBroadcast(pesanNotifikasi, JenisNotifikasi.Update, jadwal);
        }
    }
}
