using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TiketLaut.Data
{
    public class TiketLautDbContext : DbContext
    {
        public TiketLautDbContext(DbContextOptions<TiketLautDbContext> options) : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Pengguna> Penggunas { get; set; }
        public DbSet<Pelabuhan> Pelabuhans { get; set; }
        public DbSet<Kapal> Kapals { get; set; }
        public DbSet<Jadwal> Jadwals { get; set; }
        public DbSet<Tiket> Tikets { get; set; }
        public DbSet<Penumpang> Penumpangs { get; set; }
        public DbSet<RincianPenumpang> RincianPenumpangs { get; set; }
        public DbSet<DetailKendaraan> DetailKendaraans { get; set; }
        public DbSet<Notifikasi> Notifikasis { get; set; }
        public DbSet<Pembayaran> Pembayarans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure enum conversions for PostgreSQL
            var adminRoleConverter = new EnumToStringConverter<AdminRole>();
            var statusTiketConverter = new EnumToStringConverter<StatusTiket>();
            var jenisNotifikasiConverter = new EnumToStringConverter<JenisNotifikasi>();
            var jenisKendaraanConverter = new EnumToStringConverter<JenisKendaraan>();

            // Configure Admin entity
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.admin_id);
                entity.Property(e => e.nama).IsRequired().HasMaxLength(100);
                entity.Property(e => e.username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.role)
                    .HasConversion(adminRoleConverter)
                    .HasMaxLength(50);

                // Unique constraints
                entity.HasIndex(e => e.username).IsUnique();
                entity.HasIndex(e => e.email).IsUnique();
            });

            // Configure Pengguna entity
            modelBuilder.Entity<Pengguna>(entity =>
            {
                entity.HasKey(e => e.pengguna_id);
                entity.Property(e => e.nama).IsRequired().HasMaxLength(100);
                entity.Property(e => e.email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.no_hp).HasMaxLength(20);
                entity.Property(e => e.jenis_kelamin).HasMaxLength(20);
                entity.Property(e => e.kewarganegaraan).HasMaxLength(50);
                entity.Property(e => e.alamat).HasMaxLength(500);
                entity.Property(e => e.tanggal_daftar).HasDefaultValueSql("NOW()");

                // Unique constraints
                entity.HasIndex(e => e.email).IsUnique();
            });

            // Configure Pelabuhan entity
            modelBuilder.Entity<Pelabuhan>(entity =>
            {
                entity.HasKey(e => e.pelabuhan_id);
                entity.Property(e => e.nama_pelabuhan).IsRequired().HasMaxLength(100);
                entity.Property(e => e.kota).IsRequired().HasMaxLength(50);
                entity.Property(e => e.provinsi).IsRequired().HasMaxLength(50);
                entity.Property(e => e.fasilitas).HasMaxLength(1000);
                entity.Property(e => e.deskripsi).HasMaxLength(2000);
            });

            // Configure Kapal entity
            modelBuilder.Entity<Kapal>(entity =>
            {
                entity.HasKey(e => e.kapal_id);
                entity.Property(e => e.nama_kapal).IsRequired().HasMaxLength(100);
                entity.Property(e => e.fasilitas).HasMaxLength(1000);
                entity.Property(e => e.deskripsi).HasMaxLength(2000);
            });

            // Configure Jadwal entity
            modelBuilder.Entity<Jadwal>(entity =>
            {
                entity.HasKey(e => e.jadwal_id);
                entity.Property(e => e.kelas).IsRequired().HasMaxLength(50);
                entity.Property(e => e.status)
                    .HasConversion(statusTiketConverter)
                    .HasMaxLength(50);
                
                // Configure decimal precision for currency fields
                entity.Property(e => e.harga_penumpang).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_I).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_II).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_III).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_IV_A).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_IV_B).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_V_A).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_V_B).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_VI_A).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_VI_B).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_VII).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_VIII).HasPrecision(18, 2);
                entity.Property(e => e.harga_golongan_IX).HasPrecision(18, 2);

                // Configure relationships
                entity.HasOne(e => e.pelabuhan_asal)
                    .WithMany()
                    .HasForeignKey(e => e.pelabuhan_asal_id)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Jadwal_Pelabuhan_Asal");

                entity.HasOne(e => e.pelabuhan_tujuan)
                    .WithMany()
                    .HasForeignKey(e => e.pelabuhan_tujuan_id)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Jadwal_Pelabuhan_Tujuan");

                entity.HasOne(e => e.kapal)
                    .WithMany()
                    .HasForeignKey(e => e.kapal_id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.detail_kendaraans)
                    .WithOne(e => e.jadwal)
                    .HasForeignKey(e => e.jadwal_id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Tiket entity
            modelBuilder.Entity<Tiket>(entity =>
            {
                entity.HasKey(e => e.tiket_id);
                entity.Property(e => e.total_harga).HasPrecision(18, 2);
                entity.Property(e => e.status)
                    .HasConversion(statusTiketConverter)
                    .HasMaxLength(50);
                entity.Property(e => e.jenis_kendaraan_enum)
                    .HasConversion(jenisKendaraanConverter)
                    .HasMaxLength(50);
                entity.Property(e => e.plat_nomor).HasMaxLength(20);

                // Configure relationships
                entity.HasOne(e => e.jadwal)
                    .WithMany()
                    .HasForeignKey(e => e.jadwal_id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.rincianPenumpangs)
                    .WithOne(e => e.tiket)
                    .HasForeignKey(e => e.tiket_id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Penumpang entity
            modelBuilder.Entity<Penumpang>(entity =>
            {
                entity.HasKey(e => e.penumpang_id);
                entity.Property(e => e.nama).IsRequired().HasMaxLength(100);
                entity.Property(e => e.NIK_penumpang).IsRequired().HasMaxLength(16);

                // Configure relationships
                entity.HasOne(e => e.pengguna)
                    .WithMany()
                    .HasForeignKey(e => e.pengguna_id)
                    .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint for NIK
                entity.HasIndex(e => e.NIK_penumpang).IsUnique();
            });

            // Configure RincianPenumpang entity (junction table)
            modelBuilder.Entity<RincianPenumpang>(entity =>
            {
                entity.HasKey(e => e.rincian_penumpang_id);

                // Configure relationships
                entity.HasOne(e => e.tiket)
                    .WithMany(e => e.rincianPenumpangs)
                    .HasForeignKey(e => e.tiket_id)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.penumpang)
                    .WithMany()
                    .HasForeignKey(e => e.penumpang_id)
                    .OnDelete(DeleteBehavior.Restrict);

                // Prevent duplicate passenger on same ticket
                entity.HasIndex(e => new { e.tiket_id, e.penumpang_id }).IsUnique();
            });

            // Configure DetailKendaraan entity
            modelBuilder.Entity<DetailKendaraan>(entity =>
            {
                entity.HasKey(e => e.detail_kendaraan_id);
                entity.Property(e => e.jenis_kendaraan)
                    .HasConversion(jenisKendaraanConverter)
                    .HasMaxLength(50);
                entity.Property(e => e.harga_kendaraan).HasPrecision(18, 2);
                entity.Property(e => e.deskripsi).HasMaxLength(500);
                entity.Property(e => e.spesifikasi_ukuran).HasMaxLength(200);

                // Configure relationships
                entity.HasOne(e => e.jadwal)
                    .WithMany(e => e.detail_kendaraans)
                    .HasForeignKey(e => e.jadwal_id)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint to prevent duplicate vehicle types per schedule
                entity.HasIndex(e => new { e.jadwal_id, e.jenis_kendaraan }).IsUnique();
            });

            // Configure Notifikasi entity
            modelBuilder.Entity<Notifikasi>(entity =>
            {
                entity.HasKey(e => e.notifikasi_id);
                entity.Property(e => e.jenis_enum_penumpang_update_status).HasMaxLength(50);
                entity.Property(e => e.pesan).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.waktu_kirim).HasDefaultValueSql("NOW()");

                // Configure relationships
                entity.HasOne(e => e.pengguna)
                    .WithMany()
                    .HasForeignKey(e => e.pengguna_id)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.admin)
                    .WithMany()
                    .HasForeignKey(e => e.admin_id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.jadwal)
                    .WithMany()
                    .HasForeignKey(e => e.jadwal_id)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Pembayaran entity
            modelBuilder.Entity<Pembayaran>(entity =>
            {
                entity.HasKey(e => e.pembayaran_id);
                entity.Property(e => e.metode_pembayaran).IsRequired().HasMaxLength(50);
                entity.Property(e => e.jumlah_bayar).HasPrecision(18, 2);
                entity.Property(e => e.tanggal_bayar).HasDefaultValueSql("NOW()");

                // Configure relationships
                entity.HasOne(e => e.tiket)
                    .WithMany()
                    .HasForeignKey(e => e.tiket_id)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}