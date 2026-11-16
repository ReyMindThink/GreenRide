using System;

namespace GreenRide.Models
{
    public class TransportMode
    {
        // private: hanya bisa diakses dari dalam class TransportMode
        private double _faktorEmisi;

        public int TransportModeId { get; private set; }
        public string NamaModa { get; set; }

        // public: pintu resmi untuk membaca dan mengisi faktor emisi (kg CO2 per menit)
        public double FaktorEmisi
        {
            get { return _faktorEmisi; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(
                        "Faktor emisi tidak boleh kurang dari 0 kg CO2 per menit.");
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

        public void SetTransportModeId(int id)
        {
            TransportModeId = id;
        }

        // public method: menghitung emisi satu perjalanan (kg CO2) = durasi x faktor emisi
        // Memakai field private _faktorEmisi secara langsung (boleh, karena masih di dalam class ini)
        public double HitungEmisi(int durasiMenit)
        {
            if (durasiMenit <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(durasiMenit), "Durasi harus lebih dari 0 menit.");

            return durasiMenit * _faktorEmisi;
        }
    }
}