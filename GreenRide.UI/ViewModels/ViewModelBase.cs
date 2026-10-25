using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GreenRide.UI.ViewModels;

public abstract class ViewModelBase : ObservableObject { }

/// <summary>Dasar untuk halaman di dalam Shell. IsWide diisi Shell sesuai lebar jendela (adaptive).</summary>
public abstract partial class PageViewModelBase : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Span))]
    [NotifyPropertyChangedFor(nameof(SecondRow))]
    [NotifyPropertyChangedFor(nameof(SecondColumn))]
    [NotifyPropertyChangedFor(nameof(FirstMargin))]
    [NotifyPropertyChangedFor(nameof(SecondMargin))]
    private bool _isWide;

    // Layout adaptif: lebar = 2 kolom berdampingan; sempit = 1 kolom bertumpuk (panel ke-2 pindah ke baris bawah).
    public int Span => IsWide ? 1 : 2;
    public int SecondRow => IsWide ? 0 : 1;
    public int SecondColumn => IsWide ? 1 : 0;
    public Thickness FirstMargin => IsWide ? new Thickness(0, 0, 8, 0) : new Thickness(0, 0, 0, 16);
    public Thickness SecondMargin => IsWide ? new Thickness(8, 0, 0, 0) : new Thickness(0);

    public virtual void OnNavigatedTo() { }
}
