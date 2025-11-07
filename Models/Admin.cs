using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Admin")]  // Sesuai schema: public.Admin
    public class Admin
    {
        [Key]                                           // PRIMARY KEY
        public int admin_id { get; set; }               // integer GENERATED ALWAYS AS IDENTITY
        
        [Required]                                      // character varying NOT NULL
        public string nama { get; set; } = string.Empty;
        
        [Required]                                      // character varying NOT NULL UNIQUE
        public string username { get; set; } = string.Empty;
        
        [Required]                                      // character varying NOT NULL UNIQUE
        public string email { get; set; } = string.Empty;
        
        [Required]                                      // character varying NOT NULL
        public string password { get; set; } = string.Empty;
        
        [Required]                                      // character varying NOT NULL
        public string role { get; set; } = string.Empty;

        [NotMapped]                                     // Field ini tidak ada di database
        public DateTime? created_at { get; set; }       // Timestamp created (hanya untuk UI)
        
        [NotMapped]                                     // Field ini tidak ada di database
        public DateTime? updated_at { get; set; }       // Timestamp updated (hanya untuk UI)

        public bool login()
        {
            // Implementasi login admin
            return true;
        }

        public bool canCreateAdmin()
        {
            // SuperAdmin (role = "0") bisa buat admin baru 
            return role == "0";
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

        public void kirimNotifikasiBroadcast(string pesan, string jenisNotifikasi, Jadwal? jadwal = null)
        {
            // Implementasi kirim notifikasi broadcast ke semua pengguna
            var notifikasi = new Notifikasi
            {
                pengguna_id = 0, // 0 untuk broadcast
                judul_notifikasi = jenisNotifikasi,
                pesan = pesan,
                waktu_kirim = DateTime.Now,
                status_baca = false,
                admin_id = this.admin_id,
                jadwal_id = jadwal?.jadwal_id
            };
            notifikasi.kirimNotifikasi();
        }

        public void kirimNotifikasiPerubahanJadwal(Jadwal jadwal, string alasanPerubahan)
        {
            // Implementasi khusus untuk notifikasi perubahan jadwal
            string pesanNotifikasi = $"PEMBERITAHUAN PERUBAHAN JADWAL: " +
                $"Jadwal keberangkatan dari Pelabuhan ID {jadwal.pelabuhan_asal_id} " +
                $"ke Pelabuhan ID {jadwal.pelabuhan_tujuan_id} " +
                $"mengalami perubahan. Alasan: {alasanPerubahan}. " +
                $"Mohon cek aplikasi untuk update terbaru.";
            kirimNotifikasiBroadcast(pesanNotifikasi, "Update", jadwal);
        }
    }
}
