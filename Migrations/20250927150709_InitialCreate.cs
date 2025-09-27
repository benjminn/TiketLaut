using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TiketLaut.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    admin_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nama = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.admin_id);
                });

            migrationBuilder.CreateTable(
                name: "Kapals",
                columns: table => new
                {
                    kapal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nama_kapal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kapasitas_penumpang_max = table.Column<int>(type: "integer", nullable: false),
                    kapasitas_kendaraan_max = table.Column<int>(type: "integer", nullable: false),
                    fasilitas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    deskripsi = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kapals", x => x.kapal_id);
                });

            migrationBuilder.CreateTable(
                name: "Pelabuhans",
                columns: table => new
                {
                    pelabuhan_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nama_pelabuhan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kota = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provinsi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fasilitas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    deskripsi = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pelabuhans", x => x.pelabuhan_id);
                });

            migrationBuilder.CreateTable(
                name: "Penggunas",
                columns: table => new
                {
                    pengguna_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nama = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    no_hp = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    jenis_kelamin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tanggal_lahir = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    kewarganegaraan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    alamat = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tanggal_daftar = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penggunas", x => x.pengguna_id);
                });

            migrationBuilder.CreateTable(
                name: "Jadwals",
                columns: table => new
                {
                    jadwal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pelabuhan_asal_id = table.Column<int>(type: "integer", nullable: false),
                    pelabuhan_tujuan_id = table.Column<int>(type: "integer", nullable: false),
                    kapal_id = table.Column<int>(type: "integer", nullable: false),
                    kelas = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tanggal_berangkat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    waktu_berangkat = table.Column<TimeSpan>(type: "interval", nullable: false),
                    waktu_tiba = table.Column<TimeSpan>(type: "interval", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    harga_penumpang = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_I = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_II = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_III = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_IV_A = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_IV_B = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_V_A = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_V_B = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_VI_A = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_VI_B = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_VII = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_VIII = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    harga_golongan_IX = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jadwals", x => x.jadwal_id);
                    table.ForeignKey(
                        name: "FK_Jadwal_Pelabuhan_Asal",
                        column: x => x.pelabuhan_asal_id,
                        principalTable: "Pelabuhans",
                        principalColumn: "pelabuhan_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jadwal_Pelabuhan_Tujuan",
                        column: x => x.pelabuhan_tujuan_id,
                        principalTable: "Pelabuhans",
                        principalColumn: "pelabuhan_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jadwals_Kapals_kapal_id",
                        column: x => x.kapal_id,
                        principalTable: "Kapals",
                        principalColumn: "kapal_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Penumpangs",
                columns: table => new
                {
                    penumpang_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pengguna_id = table.Column<int>(type: "integer", nullable: false),
                    nama = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NIK_penumpang = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penumpangs", x => x.penumpang_id);
                    table.ForeignKey(
                        name: "FK_Penumpangs_Penggunas_pengguna_id",
                        column: x => x.pengguna_id,
                        principalTable: "Penggunas",
                        principalColumn: "pengguna_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetailKendaraans",
                columns: table => new
                {
                    detail_kendaraan_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    jadwal_id = table.Column<int>(type: "integer", nullable: false),
                    jenis_kendaraan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    harga_kendaraan = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bobot_unit = table.Column<int>(type: "integer", nullable: false),
                    deskripsi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    spesifikasi_ukuran = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailKendaraans", x => x.detail_kendaraan_id);
                    table.ForeignKey(
                        name: "FK_DetailKendaraans_Jadwals_jadwal_id",
                        column: x => x.jadwal_id,
                        principalTable: "Jadwals",
                        principalColumn: "jadwal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifikasis",
                columns: table => new
                {
                    notifikasi_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pengguna_id = table.Column<int>(type: "integer", nullable: false),
                    admin_id = table.Column<int>(type: "integer", nullable: false),
                    jenis_enum_penumpang_update_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pesan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    waktu_kirim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    status_baca = table.Column<bool>(type: "boolean", nullable: false),
                    jadwal_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifikasis", x => x.notifikasi_id);
                    table.ForeignKey(
                        name: "FK_Notifikasis_Admins_admin_id",
                        column: x => x.admin_id,
                        principalTable: "Admins",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifikasis_Jadwals_jadwal_id",
                        column: x => x.jadwal_id,
                        principalTable: "Jadwals",
                        principalColumn: "jadwal_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifikasis_Penggunas_pengguna_id",
                        column: x => x.pengguna_id,
                        principalTable: "Penggunas",
                        principalColumn: "pengguna_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tikets",
                columns: table => new
                {
                    tiket_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    jadwal_id = table.Column<int>(type: "integer", nullable: false),
                    total_harga = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tanggal_pemesanan = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    jenis_kendaraan_enum = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    plat_nomor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pengguna_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tikets", x => x.tiket_id);
                    table.ForeignKey(
                        name: "FK_Tikets_Jadwals_jadwal_id",
                        column: x => x.jadwal_id,
                        principalTable: "Jadwals",
                        principalColumn: "jadwal_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tikets_Penggunas_pengguna_id",
                        column: x => x.pengguna_id,
                        principalTable: "Penggunas",
                        principalColumn: "pengguna_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pembayarans",
                columns: table => new
                {
                    pembayaran_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tiket_id = table.Column<int>(type: "integer", nullable: false),
                    metode_pembayaran = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    jumlah_bayar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tanggal_bayar = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pembayarans", x => x.pembayaran_id);
                    table.ForeignKey(
                        name: "FK_Pembayarans_Tikets_tiket_id",
                        column: x => x.tiket_id,
                        principalTable: "Tikets",
                        principalColumn: "tiket_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RincianPenumpangs",
                columns: table => new
                {
                    rincian_penumpang_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tiket_id = table.Column<int>(type: "integer", nullable: false),
                    penumpang_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RincianPenumpangs", x => x.rincian_penumpang_id);
                    table.ForeignKey(
                        name: "FK_RincianPenumpangs_Penumpangs_penumpang_id",
                        column: x => x.penumpang_id,
                        principalTable: "Penumpangs",
                        principalColumn: "penumpang_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RincianPenumpangs_Tikets_tiket_id",
                        column: x => x.tiket_id,
                        principalTable: "Tikets",
                        principalColumn: "tiket_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_email",
                table: "Admins",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_username",
                table: "Admins",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetailKendaraans_jadwal_id_jenis_kendaraan",
                table: "DetailKendaraans",
                columns: new[] { "jadwal_id", "jenis_kendaraan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jadwals_kapal_id",
                table: "Jadwals",
                column: "kapal_id");

            migrationBuilder.CreateIndex(
                name: "IX_Jadwals_pelabuhan_asal_id",
                table: "Jadwals",
                column: "pelabuhan_asal_id");

            migrationBuilder.CreateIndex(
                name: "IX_Jadwals_pelabuhan_tujuan_id",
                table: "Jadwals",
                column: "pelabuhan_tujuan_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifikasis_admin_id",
                table: "Notifikasis",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifikasis_jadwal_id",
                table: "Notifikasis",
                column: "jadwal_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notifikasis_pengguna_id",
                table: "Notifikasis",
                column: "pengguna_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pembayarans_tiket_id",
                table: "Pembayarans",
                column: "tiket_id");

            migrationBuilder.CreateIndex(
                name: "IX_Penggunas_email",
                table: "Penggunas",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Penumpangs_NIK_penumpang",
                table: "Penumpangs",
                column: "NIK_penumpang",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Penumpangs_pengguna_id",
                table: "Penumpangs",
                column: "pengguna_id");

            migrationBuilder.CreateIndex(
                name: "IX_RincianPenumpangs_penumpang_id",
                table: "RincianPenumpangs",
                column: "penumpang_id");

            migrationBuilder.CreateIndex(
                name: "IX_RincianPenumpangs_tiket_id_penumpang_id",
                table: "RincianPenumpangs",
                columns: new[] { "tiket_id", "penumpang_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tikets_jadwal_id",
                table: "Tikets",
                column: "jadwal_id");

            migrationBuilder.CreateIndex(
                name: "IX_Tikets_pengguna_id",
                table: "Tikets",
                column: "pengguna_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetailKendaraans");

            migrationBuilder.DropTable(
                name: "Notifikasis");

            migrationBuilder.DropTable(
                name: "Pembayarans");

            migrationBuilder.DropTable(
                name: "RincianPenumpangs");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Penumpangs");

            migrationBuilder.DropTable(
                name: "Tikets");

            migrationBuilder.DropTable(
                name: "Jadwals");

            migrationBuilder.DropTable(
                name: "Penggunas");

            migrationBuilder.DropTable(
                name: "Pelabuhans");

            migrationBuilder.DropTable(
                name: "Kapals");
        }
    }
}
