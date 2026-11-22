using System;

namespace GreenRide.Models
{
    public class TransportMode
    {
        public int TransportModeId { get; private set; }
        private string _namaModa = "";
        private double _faktorEmisi;

        public string NamaModa
        {
            get { return _namaModa; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Nama moda tidak boleh kosong.");
                _namaModa = value;
            }
        }

        public double FaktorEmisi // kg CO2 per menit penggunaan
        {
            get { return _faktorEmisi; }
            set
            {
                if (value < 0)
                    throw new InvalidOperationException("Faktor emisi tidak boleh kurang dari 0.");
                _faktorEmisi = value;
            }
        }

        // Konstruktor untuk data yang sudah ada di database
        public TransportMode(int transportModeId, string namaModa, double faktorEmisi)
        {
            TransportModeId = transportModeId;
            NamaModa = namaModa;
            FaktorEmisi = faktorEmisi;
        }

        // Konstruktor untuk data baru (id belum ada)
        public TransportMode(string namaModa, double faktorEmisi)
        {
            NamaModa = namaModa;
            FaktorEmisi = faktorEmisi;
        }

        // virtual: kelas turunan boleh mengganti cara menghitung emisi
        public virtual double HitungEmisi(int durasiMenit)
        {
            return FaktorEmisi * durasiMenit;
        }

        public static TransportMode Buat(int id, string nama, double faktor)
        {
            string n = nama.ToLowerInvariant();

            if (n.Contains("jalan") || n.Contains("sepeda") && !n.Contains("motor"))
                return new TanpaEmisi(id, nama);
            if (n.Contains("umum") || n.Contains("bus") || n.Contains("kereta"))
                return new KendaraanUmum(id, nama, faktor);

            return new KendaraanPribadi(id, nama, faktor);
        }
    }


}
