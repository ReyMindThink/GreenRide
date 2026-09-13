using System;
using System.Collections.Generic;
using System.Linq;

namespace GreenRide.Services
{
    using GreenRide.Models;

    // proses evaluasi & pemberian badge mingguan berdasarkan WeeklyEmission user.
    public class BadgeService
    {
        private readonly List<Badge> _katalogBadge;
        private readonly List<UserBadge> _riwayatUserBadge;

        public BadgeService(List<Badge> katalogBadge, List<UserBadge> riwayatUserBadge)
        {
            _katalogBadge = katalogBadge;
            _riwayatUserBadge = riwayatUserBadge;
        }

        // Mengembalikan UserBadge baru jika user berhasil dapat badge, 
        // atau null jika belum memenuhi syarat.
        public UserBadge? EvaluasiDanBerikanBadge(User user, WeeklyEmission weeklyEmission)
        {
            if (weeklyEmission.SudahDievaluasiBadge)
            {
                return null;
            }

            // Ambil badge dengan syarat paling ketat (SyaratMaksEmisi terkecil) yang terpenuhi
            Badge badgeLayak = _katalogBadge
                .Where(b => b.CekKelayakan(weeklyEmission))
                .OrderBy(b => b.SyaratMaksEmisi)
                .FirstOrDefault();

            weeklyEmission.TandaiSudahDievaluasi();

            if (badgeLayak == null)
            {
                return null; // belum memenuhi syarat badge apapun minggu ini
            }

            var userBadgeBaru = new UserBadge(user.UserId, badgeLayak.BadgeId, DateTime.Now);
            _riwayatUserBadge.Add(userBadgeBaru);

            return userBadgeBaru;
        }
    }
}
