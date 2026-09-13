using System;

namespace GreenRide.Models
{
    public class TransportMode
    {
        public int TransportModeId { get; private set; }
        public string NamaModa { get; set; }
        public double FaktorEmisi { get; set; } // kg CO2 per menit penggunaan

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
    }
}
