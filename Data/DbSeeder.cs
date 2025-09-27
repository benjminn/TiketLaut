using Microsoft.EntityFrameworkCore;

namespace TiketLaut.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(TiketLautDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Starting database seeding...");

                // Seed Pelabuhans
                if (!await context.Pelabuhans.AnyAsync())
                {
                    logger.LogInformation("Seeding Pelabuhans...");
                    var pelabuhans = new List<Pelabuhan>
                    {
                        new Pelabuhan
                        {
                            nama_pelabuhan = "Pelabuhan Merak",
                            kota = "Cilegon",
                            provinsi = "Banten",
                            fasilitas = "Parkir, Toilet, Mushola, Kantin, ATM",
                            deskripsi = "Pelabuhan utama penghubung Pulau Jawa dan Sumatera di Selat Sunda"
                        },
                        new Pelabuhan
                        {
                            nama_pelabuhan = "Pelabuhan Bakauheni",
                            kota = "Lampung Selatan",
                            provinsi = "Lampung",
                            fasilitas = "Parkir, Toilet, Mushola, Kantin, ATM, Ruang VIP",
                            deskripsi = "Pelabuhan tersibuk di Lampung untuk penyeberangan ke Pulau Jawa"
                        },
                        new Pelabuhan
                        {
                            nama_pelabuhan = "Pelabuhan Gilimanuk",
                            kota = "Jembrana",
                            provinsi = "Bali",
                            fasilitas = "Parkir, Toilet, Mushola, Restoran, Toko Souvenir",
                            deskripsi = "Pelabuhan di ujung barat Pulau Bali penghubung ke Pulau Jawa"
                        },
                        new Pelabuhan
                        {
                            nama_pelabuhan = "Pelabuhan Ketapang",
                            kota = "Banyuwangi",
                            provinsi = "Jawa Timur",
                            fasilitas = "Parkir, Toilet, Mushola, Kantin, Mini Market",
                            deskripsi = "Pelabuhan di ujung timur Pulau Jawa penghubung ke Pulau Bali"
                        }
                    };

                    context.Pelabuhans.AddRange(pelabuhans);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} pelabuhans", pelabuhans.Count);
                }

                // Seed Kapals
                if (!await context.Kapals.AnyAsync())
                {
                    logger.LogInformation("Seeding Kapals...");
                    var kapals = new List<Kapal>
                    {
                        new Kapal
                        {
                            nama_kapal = "KMP Legundi",
                            kapasitas_penumpang_max = 500,
                            kapasitas_kendaraan_max = 50,
                            fasilitas = "AC, Mushola, Kantin, Ruang VIP",
                            deskripsi = "Kapal ferry modern dengan fasilitas lengkap"
                        },
                        new Kapal
                        {
                            nama_kapal = "KMP Sebesi",
                            kapasitas_penumpang_max = 600,
                            kapasitas_kendaraan_max = 60,
                            fasilitas = "AC, Mushola, Restoran, Ruang VIP, Wifi",
                            deskripsi = "Kapal ferry berkecepatan tinggi dengan kenyamanan premium"
                        },
                        new Kapal
                        {
                            nama_kapal = "KMP Dharmasaba",
                            kapasitas_penumpang_max = 450,
                            kapasitas_kendaraan_max = 45,
                            fasilitas = "AC, Mushola, Kantin",
                            deskripsi = "Kapal ferry reguler untuk rute Bali-Jawa"
                        }
                    };

                    context.Kapals.AddRange(kapals);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} kapals", kapals.Count);
                }

                // Seed Admin
                if (!await context.Admins.AnyAsync())
                {
                    logger.LogInformation("Seeding Admins...");
                    var admins = new List<Admin>
                    {
                        new Admin
                        {
                            nama = "Super Admin",
                            username = "superadmin",
                            email = "superadmin@tiketlaut.com",
                            password = "superadmin123", // In production, this should be hashed
                            role = AdminRole.SuperAdmin
                        },
                        new Admin
                        {
                            nama = "Operation Admin",
                            username = "admin",
                            email = "admin@tiketlaut.com",
                            password = "admin123", // In production, this should be hashed
                            role = AdminRole.OperationAdmin
                        }
                    };

                    context.Admins.AddRange(admins);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} admins", admins.Count);
                }

                // Seed sample Jadwals with all relationships
                if (!await context.Jadwals.AnyAsync())
                {
                    logger.LogInformation("Seeding Jadwals...");
                    
                    // Get the first pelabuhan and kapal for relationships
                    var pelabuhanAsal = await context.Pelabuhans.FirstAsync();
                    var pelabuhanTujuan = await context.Pelabuhans.Skip(1).FirstAsync();
                    var kapal = await context.Kapals.FirstAsync();

                    var jadwals = new List<Jadwal>
                    {
                        new Jadwal
                        {
                            pelabuhan_asal_id = pelabuhanAsal.pelabuhan_id,
                            pelabuhan_tujuan_id = pelabuhanTujuan.pelabuhan_id,
                            kapal_id = kapal.kapal_id,
                            kelas = "Ekonomi",
                            tanggal_berangkat = DateTime.Today.AddDays(1),
                            waktu_berangkat = new TimeSpan(8, 0, 0),
                            waktu_tiba = new TimeSpan(10, 30, 0),
                            status = StatusTiket.Tersedia,
                            harga_penumpang = 15000m,
                            harga_golongan_I = 20000m,
                            harga_golongan_II = 25000m,
                            harga_golongan_III = 30000m,
                            harga_golongan_IV_A = 85000m,
                            harga_golongan_IV_B = 95000m,
                            harga_golongan_V_A = 125000m,
                            harga_golongan_V_B = 145000m,
                            harga_golongan_VI_A = 165000m,
                            harga_golongan_VI_B = 185000m,
                            harga_golongan_VII = 225000m,
                            harga_golongan_VIII = 285000m,
                            harga_golongan_IX = 345000m
                        }
                    };

                    context.Jadwals.AddRange(jadwals);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} jadwals", jadwals.Count);
                }

                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during database seeding");
                throw;
            }
        }
    }
}