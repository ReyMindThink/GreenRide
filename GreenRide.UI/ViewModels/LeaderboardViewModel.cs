using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GreenRide.UI.Helpers;
using GreenRide.UI.Models;
using GreenRide.UI.Services;

namespace GreenRide.UI.ViewModels;

/// <summary>Satu kartu di podium (peringkat 1–3).</summary>
public sealed class PodiumItem
{
    public PodiumItem(LeaderboardEntry e)
    {
        Rank = e.Rank;
        RankText = e.Rank.ToString();
        Nama = e.Nama;
        NamaText = e.IsCurrentUser ? e.Nama + " (Kamu)" : e.Nama;
        EmisiText = Fmt.Co2(e.TotalEmisiKg);
        var def = BadgeCatalog.Get(e.Badge);
        IconUri = def is null ? null : AssetUri.Of(def.IconUri);
        BadgeName = def?.Nama ?? "Belum ada badge";
        IsFirst = e.Rank == 1;
        IsMe = e.IsCurrentUser;
    }

    public int Rank { get; }
    public string RankText { get; }
    public string Nama { get; }
    public string NamaText { get; }
    public string EmisiText { get; }
    public string? IconUri { get; }
    public bool HasIcon => IconUri is not null;
    public string BadgeName { get; }
    public bool IsFirst { get; }
    public bool IsMe { get; }
}

/// <summary>Satu baris tabel peringkat.</summary>
public sealed class LeaderRowItem
{
    public LeaderRowItem(LeaderboardEntry e)
    {
        RankText = e.Rank.ToString();
        Nama = e.Nama;
        EmisiText = Fmt.Co2(e.TotalEmisiKg);
        BadgeName = BadgeCatalog.Get(e.Badge)?.Nama ?? "—";
        IsMe = e.IsCurrentUser;
        IsNotMe = !e.IsCurrentUser;
        DetailText = $"{BadgeName} · {EmisiText}";
    }

    public string RankText { get; }
    public string Nama { get; }
    public string EmisiText { get; }
    public string BadgeName { get; }
    public string DetailText { get; }
    public bool IsMe { get; }
    public bool IsNotMe { get; }
}

/// <summary>Leaderboard minggu berjalan (PRD 5.7): emisi terendah = peringkat 1, tampil top 10.</summary>
public sealed partial class LeaderboardViewModel : ViewModelBase, INavigationAware
{
    private const int TopCount = 10;

    private readonly ILeaderboardService _leaderboard;
    private readonly ISessionService _session;
    private readonly IClock _clock;

    public LeaderboardViewModel(ILeaderboardService leaderboard, ISessionService session, IClock clock)
    {
        _leaderboard = leaderboard;
        _session = session;
        _clock = clock;
    }

    public ObservableCollection<PodiumItem> Podium { get; } = new();
    public ObservableCollection<LeaderRowItem> Rows { get; } = new();

    [ObservableProperty] private string _subtitle = "";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasData;

    public async Task OnNavigatedToAsync()
    {
        if (_session.CurrentUser is not { } user) return;

        var monday = WeekHelper.MondayOf(_clock.Today);
        Subtitle = $"Minggu ini · {Fmt.WeekRange(monday, monday.AddDays(6))}";

        try
        {
            var all = await _leaderboard.GetCurrentWeekAsync(user.Id);
            ErrorMessage = null;

            var top = all.Take(TopCount).ToList();
            Podium.Clear();
            Rows.Clear();

            // Urutan tampil: peringkat 2, 1, 3 (juara di tengah).
            foreach (var rank in new[] { 2, 1, 3 })
                if (top.FirstOrDefault(e => e.Rank == rank) is { } e)
                    Podium.Add(new PodiumItem(e));

            foreach (var e in top.Where(e => e.Rank > 3))
                Rows.Add(new LeaderRowItem(e));

            // Pengguna di luar 10 besar tetap melihat posisinya sendiri.
            if (all.FirstOrDefault(e => e.IsCurrentUser) is { } me && me.Rank > TopCount)
                Rows.Add(new LeaderRowItem(me));

            HasData = all.Count > 0;
        }
        catch (ServiceException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
