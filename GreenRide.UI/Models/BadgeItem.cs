namespace GreenRide.UI.Models;

/// <summary>
/// Badge untuk ditampilkan. IsEarned berasal dari ada/tidaknya baris UserBadge milik user.
/// Jika belum diraih, IconKind otomatis menjadi "Locked" (abu-abu) lewat BadgeIcon.
/// </summary>
public sealed class BadgeItem
{
    public BadgeDefinition Definition { get; }
    public bool IsEarned { get; }

    public BadgeItem(BadgeDefinition definition, bool isEarned)
    {
        Definition = definition;
        IsEarned = isEarned;
    }

    public string IconKind => IsEarned ? Definition.IconKind : "Locked";
    public string Title => IsEarned ? Definition.Nama : "Belum diraih";
    public string Subtitle => IsEarned ? Definition.Syarat : "Terkunci · " + Definition.Nama;
}
