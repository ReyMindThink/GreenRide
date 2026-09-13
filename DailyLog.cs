using System;

namespace GreenRide.Models
{
    public class DailyLog
    {
        public int DailyLogId { get; private set; }
        public int UserId { get; private set; }
        public DateTime Tanggal { get; private set; }
        public int Durasi { get; private set; } // dalam menit
        public int TransportModeId { get; private set; }

        public DailyLog(int userId, DateTime tanggal, int durasi, int transportModeId)
        {
            UserId = userId;
            Tanggal = tanggal;
            Durasi = durasi;
            TransportModeId = transportModeId;
        }

        public void SetDailyLogId(int id)
        {
            DailyLogId = id;
        }

        // Mengganti moda transportasi & durasi untuk log yang sudah tercatat 
        // dalam rentang 1 hari
        public void RevisiModa(TransportMode modaBaru)
        {
            TransportModeId = modaBaru.TransportModeId;
        }

        // moda yang diberikan harus sesuai dengan TransportModeId milik log ini,
        // supaya class ini tidak perlu tahu cara mengambil data dari database sendiri.
        public double HitungEmisiHarian(TransportMode moda)
        {
            if (moda.TransportModeId != TransportModeId)
            {
                throw new ArgumentException(
                    "TransportMode yang diberikan tidak sesuai dengan moda pada log ini.");
            }

            return Durasi * moda.FaktorEmisi;
        }
    }
}
