using Microsoft.EntityFrameworkCore;

namespace TiketLaut.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Pengguna> Penggunas { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Pelabuhan> Pelabuhans { get; set; }
        public DbSet<Kapal> Kapals { get; set; }
        public DbSet<Jadwal> Jadwals { get; set; }
        public DbSet<Tiket> Tikets { get; set; }
        public DbSet<Penumpang> Penumpangs { get; set; }
        public DbSet<RincianPenumpang> RincianPenumpangs { get; set; }
        public DbSet<Pembayaran> Pembayarans { get; set; }
        public DbSet<DetailKendaraan> DetailKendaraans { get; set; }
        public DbSet<Notifikasi> Notifikasis { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<Pengguna>().ToTable("Pengguna");
            modelBuilder.Entity<Admin>().ToTable("Admin");
            modelBuilder.Entity<Pelabuhan>().ToTable("Pelabuhan");
            modelBuilder.Entity<Kapal>().ToTable("Kapal");
            modelBuilder.Entity<Jadwal>().ToTable("Jadwal");
            modelBuilder.Entity<Tiket>().ToTable("Tiket");
            modelBuilder.Entity<Penumpang>().ToTable("Penumpang");
            modelBuilder.Entity<RincianPenumpang>().ToTable("RincianPenumpang");
            modelBuilder.Entity<Pembayaran>().ToTable("Pembayaran");
            modelBuilder.Entity<DetailKendaraan>().ToTable("DetailKendaraan");
            modelBuilder.Entity<Notifikasi>().ToTable("Notifikasi");

            modelBuilder.Entity<Pengguna>()
                .Property(p => p.alamat)
                .IsRequired(false);

            modelBuilder.Entity<Jadwal>()
                .HasOne(j => j.pelabuhan_asal)
                .WithMany(p => p.JadwalAsals)
                .HasForeignKey(j => j.pelabuhan_asal_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Jadwal>()
                .HasOne(j => j.pelabuhan_tujuan)
                .WithMany(p => p.JadwalTujuans)
                .HasForeignKey(j => j.pelabuhan_tujuan_id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tiket>()
                .Property(t => t.total_harga)
                .HasColumnType("numeric");

            modelBuilder.Entity<Pembayaran>()
                .Property(p => p.jumlah_bayar)
                .HasColumnType("numeric");

            modelBuilder.Entity<DetailKendaraan>()
                .Property(d => d.harga_kendaraan)
                .HasColumnType("numeric");

            modelBuilder.Entity<Pengguna>()
                .HasIndex(p => p.email)
                .IsUnique();
        }
    }
}