using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly Action _onLogout;

    public AccountInfo Account { get; }
    public DashboardViewModel Dashboard { get; }
    public CatatViewModel Catat { get; }
    public RingkasanViewModel Ringkasan { get; }
    public ObservableCollection<NavItem> NavItems { get; }

    [ObservableProperty] private PageViewModelBase _currentPage;

    /// <summary>Diisi ShellView berdasarkan lebar jendela: true → navigasi samping + 2 kolom.</summary>
    [ObservableProperty] private bool _isWide;

    public ShellViewModel(IGreenRideRepository repo, AccountInfo account, Action onLogout)
    {
        Account = account;
        _onLogout = onLogout;

        var gami = new GamificationService(repo);
        Dashboard = new DashboardViewModel(repo, gami, account);
        Catat = new CatatViewModel(repo, gami, account);
        Ringkasan = new RingkasanViewModel(repo, gami, account);
        Dashboard.RequestCatat = () => Select(NavItems[1]);

        NavItems = new ObservableCollection<NavItem>
        {
            new("Beranda",   "🏠", Dashboard, Select),
            new("Catat",     "➕", Catat,     Select),
            new("Ringkasan", "📊", Ringkasan, Select),
        };

        _currentPage = Dashboard;
        Select(NavItems[0]);
    }

    private void Select(NavItem item)
    {
        foreach (var n in NavItems) n.IsSelected = n == item;
        CurrentPage = item.Page;
        item.Page.OnNavigatedTo();
    }

    partial void OnIsWideChanged(bool value)
    {
        Dashboard.IsWide = value;
        Catat.IsWide = value;
        Ringkasan.IsWide = value;
    }

    [RelayCommand] private void Keluar() => _onLogout();
}
