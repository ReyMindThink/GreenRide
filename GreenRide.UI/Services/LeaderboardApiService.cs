using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GreenRide.UI.Helpers;
using GreenRide.UI.Models;

namespace GreenRide.UI.Services;

/// <summary>
/// Leaderboard lewat layanan web (REST API Supabase, fungsi RPC "leaderboard_minggu_ini").
/// Bila API belum dikonfigurasi atau gagal dijangkau, otomatis memakai <paramref name="fallback"/>
/// (akses database langsung), sehingga leaderboard tetap tampil.
/// Konfigurasi di .env: GREENRIDE_API_URL (https://xxxx.supabase.co) dan GREENRIDE_API_KEY (anon key).
/// </summary>
public sealed class LeaderboardApiService : ILeaderboardService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    private readonly ILeaderboardService _fallback;
    private readonly IClock _clock;

    public LeaderboardApiService(ILeaderboardService fallback, IClock clock)
    {
        _fallback = fallback;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetCurrentWeekAsync(Guid userId)
    {
        var baseUrl = Environment.GetEnvironmentVariable("GREENRIDE_API_URL")?.TrimEnd('/');
        var apiKey = Environment.GetEnvironmentVariable("GREENRIDE_API_KEY");
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
            return await _fallback.GetCurrentWeekAsync(userId);

        try
        {
            var senin = WeekHelper.MondayOf(_clock.Today);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/rest/v1/rpc/leaderboard_minggu_ini")
            {
                Content = JsonContent.Create(new
                {
                    p_awal = senin.ToString("yyyy-MM-dd"),
                    p_user_id = userId
                })
            };
            req.Headers.Add("apikey", apiKey);
            // Kunci lama (anon, berformat JWT "eyJ...") dikirim juga sebagai Bearer; kunci baru
            // "sb_publishable_..." cukup lewat header apikey.
            if (apiKey.StartsWith("eyJ", StringComparison.Ordinal))
                req.Headers.Add("Authorization", "Bearer " + apiKey);

            using var res = await Http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            var rows = await res.Content.ReadFromJsonAsync<List<Row>>() ?? new List<Row>();

            return rows.Select(r => new LeaderboardEntry(
                Rank: r.Peringkat,
                Nama: r.Nama,
                TotalEmisiKg: r.Total,
                Badge: BadgeRules.FromTotal(r.Total),
                IsCurrentUser: r.IsCurrentUser)).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            Console.WriteLine("LEADERBOARD API ERROR, memakai database langsung: " + ex.Message);
            return await _fallback.GetCurrentWeekAsync(userId);
        }
    }

    private sealed class Row
    {
        [JsonPropertyName("peringkat")] public int Peringkat { get; set; }
        [JsonPropertyName("nama")] public string Nama { get; set; } = "";
        [JsonPropertyName("total")] public decimal Total { get; set; }
        [JsonPropertyName("is_current_user")] public bool IsCurrentUser { get; set; }
    }
}
