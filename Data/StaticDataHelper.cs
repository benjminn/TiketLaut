namespace TiketLaut.Data
{
    /// <summary>
    /// Temporary static data helper for controllers that haven't been migrated to use DbContext yet
    /// This will be removed once all controllers are updated to use the database
    /// </summary>
    public static class StaticDataHelper
    {
        public static readonly List<object> AllPelabuhanData = new List<object>
        {
            new {
                pelabuhan_id = 1,
                nama_pelabuhan = "Pelabuhan Merak",
                kota = "Cilegon",
                provinsi = "Banten",
                fasilitas = "Parkir, Toilet, Mushola, Kantin, ATM",
                deskripsi = "Pelabuhan utama penghubung Pulau Jawa dan Sumatera di Selat Sunda"
            },
            new {
                pelabuhan_id = 2,
                nama_pelabuhan = "Pelabuhan Bakauheni",
                kota = "Lampung Selatan",
                provinsi = "Lampung",
                fasilitas = "Parkir, Toilet, Mushola, Kantin, ATM, Ruang VIP",
                deskripsi = "Pelabuhan tersibuk di Lampung untuk penyeberangan ke Pulau Jawa"
            },
            new {
                pelabuhan_id = 3,
                nama_pelabuhan = "Pelabuhan Gilimanuk",
                kota = "Jembrana",
                provinsi = "Bali",
                fasilitas = "Parkir, Toilet, Mushola, Restoran, Toko Souvenir",
                deskripsi = "Pelabuhan di ujung barat Pulau Bali penghubung ke Pulau Jawa"
            },
            new {
                pelabuhan_id = 4,
                nama_pelabuhan = "Pelabuhan Ketapang",
                kota = "Banyuwangi",
                provinsi = "Jawa Timur",
                fasilitas = "Parkir, Toilet, Mushola, Kantin, Mini Market",
                deskripsi = "Pelabuhan di ujung timur Pulau Jawa penghubung ke Pulau Bali"
            },
            new {
                pelabuhan_id = 5,
                nama_pelabuhan = "Pelabuhan Lembar",
                kota = "Lombok Barat",
                provinsi = "Nusa Tenggara Barat",
                fasilitas = "Parkir, Toilet, Mushola, Restoran, ATM, Wifi",
                deskripsi = "Pelabuhan utama di Pulau Lombok untuk penyeberangan antar pulau"
            },
            new {
                pelabuhan_id = 6,
                nama_pelabuhan = "Pelabuhan Padangbai",
                kota = "Karangasem",
                provinsi = "Bali",
                fasilitas = "Parkir, Toilet, Mushola, Warung, Penginapan",
                deskripsi = "Pelabuhan di timur Bali penghubung ke Lombok dan Gili"
            }
        };
    }
}