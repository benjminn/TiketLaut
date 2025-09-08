using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KapalKlik
{
    // Enum untuk jenis kendaraan
    public enum JenisKendaraan
    {
        Sepeda_Motor,
        Mobil,
        Truk,
        Bus
    }

    // Enum untuk status tiket
    public enum StatusTiket
    {
        Successful,
        Pending,
        Cancelled
    }

    // Enum untuk jenis notifikasi
    public enum JenisNotifikasi
    {
        Pengingatkan,
        Update,
        Status
    }
}