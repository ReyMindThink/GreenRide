using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>
/// Layar sementara untuk layar yang baru dibuat di tahap berikutnya.
/// Sekarang dipakai sebagai tujuan setelah login (sampai Shell dibuat di tahap 3).
/// </summary>
public sealed partial class PlaceholderViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;

    public PlaceholderViewModel(INavigationService navigation) => _navigation = navigation;

    public string Title { get; set; } = "";
    public string Message { get; set; } = "Layar ini dibuat di tahap berikutnya.";
    public string BackLabel { get; set; } = "Kembali";

    /// <summary>Aksi tombol bawah. Bila kosong, kembali ke Layar Awal.</summary>
    public Action? OnBack { get; set; }

    [RelayCommand]
    private void Back()
    {
        if (OnBack is not null) OnBack();
        else _navigation.NavigateTo<IntroViewModel>();
    }
}
