using CommunityToolkit.Mvvm.ComponentModel;
using GreenRide.Models;

namespace GreenRide.UI.Models;

public sealed partial class TransportOption : ObservableObject
{
    public TransportMode Mode { get; }
    public int ModaId => Mode.TransportModeId;
    public string Nama => Mode.NamaModa;
    public string FaktorText => Mode.FaktorEmisi <= 0
        ? "0 emisi"
        : Mode.FaktorEmisi.ToString("0.00", Fmt.Id) + " kg/mnt";

    /// <summary>True jika ini moda dari perjalanan yang sedang berjalan (ModaId == ActiveModaId).</summary>
    [ObservableProperty] private bool _isActive;

    /// <summary>True jika ada perjalanan aktif dan ini bukan modanya.</summary>
    [ObservableProperty] private bool _isDisabled;

    public TransportOption(TransportMode mode)
    {
        Mode = mode;
    }
}
