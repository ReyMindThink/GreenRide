using System;

namespace GreenRide.Models
{
    // Motor, mobil: memakai rumus dasar
    public class KendaraanPribadi : TransportMode
    {
        public KendaraanPribadi(int id, string nama, double faktor)
            : base(id, nama, faktor) { }

        public KendaraanPribadi(string nama, double faktor)
            : base(nama, faktor) { }
    }

    // Bus, kereta, dll: emisi dibagi rata-rata penumpang
    public class KendaraanUmum : TransportMode
    {
        private const int RataRataPenumpang = 20; // asumsi, sesuaikan

        public KendaraanUmum(int id, string nama, double faktor)
            : base(id, nama, faktor) { }

        public KendaraanUmum(string nama, double faktor)
            : base(nama, faktor) { }

        public override double HitungEmisi(int durasiMenit)
        {
            return base.HitungEmisi(durasiMenit) / RataRataPenumpang;
        }
    }

    // Berjalan kaki, sepeda: tidak ada emisi
    public class TanpaEmisi : TransportMode
    {
        public TanpaEmisi(int id, string nama)
            : base(id, nama, 0) { }

        public TanpaEmisi(string nama)
            : base(nama, 0) { }

        public override double HitungEmisi(int durasiMenit)
        {
            return 0;
        }
    }
}