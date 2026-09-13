using System;

namespace GreenRide.Models
{
    public class Badge
    {
        public int BadgeId { get; private set; }
        public string NamaBadge { get; set; }
        public double SyaratMaksEmisi { get; set; }

        public Badge(int badgeId, string namaBadge, double syaratMaksEmisi)
        {
            BadgeId = badgeId;
            NamaBadge = namaBadge;
            SyaratMaksEmisi = syaratMaksEmisi;
        }

        public void SetBadgeId(int id)
        {
            BadgeId = id;
        }

        public bool CekKelayakan(WeeklyEmission weeklyEmission)
        {
            return weeklyEmission.TotalEmisi <= SyaratMaksEmisi;
        }
    }
}
