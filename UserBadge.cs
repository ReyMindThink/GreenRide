using System;

namespace GreenRide.Models
{
    // Baris baru dibuat setiap kali User berhasil meraih sebuah Badge pada minggu tertentu.
    public class UserBadge
    {
        public int UserBadgeId { get; private set; }
        public int UserId { get; private set; }
        public int BadgeId { get; private set; }
        public DateTime TanggalDiperoleh { get; private set; }

        public UserBadge(int userId, int badgeId, DateTime tanggalDiperoleh)
        {
            UserId = userId;
            BadgeId = badgeId;
            TanggalDiperoleh = tanggalDiperoleh;
        }

        public void SetUserBadgeId(int id)
        {
            UserBadgeId = id;
        }
    }
}
