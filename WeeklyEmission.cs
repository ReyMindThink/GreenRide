using System;

namespace GreenRide.Models
{
    public class WeeklyEmission
    {
        public int WeeklyEmissionId { get; private set; }
        public int UserId { get; private set; }
        public DateTime TanggalMulai { get; private set; }
        public double TotalEmisi { get; private set; }

        // Penanda apakah minggu ini sudah pernah dievaluasi untuk pemberian badge.
        public bool SudahDievaluasiBadge { get; private set; }

        public WeeklyEmission(int userId, DateTime tanggalMulai)
        {
            UserId = userId;
            TanggalMulai = tanggalMulai;
            TotalEmisi = 0;
            SudahDievaluasiBadge = false;
        }

        public void SetWeeklyEmissionId(int id)
        {
            WeeklyEmissionId = id;
        }

        // Dipanggil setiap kali ada DailyLog baru/direvisi dalam minggu ini
        public void TambahEmisiHarian(double emisiHarian)
        {
            TotalEmisi += emisiHarian;
        }

        public double HitungTotalEmisi()
        {
            return TotalEmisi;
        }

        public void TandaiSudahDievaluasi()
        {
            SudahDievaluasiBadge = true;
        }
    }
}
