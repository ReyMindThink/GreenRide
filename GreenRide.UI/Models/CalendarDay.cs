using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GreenRide.UI.Models;

public partial class CalendarDay : ObservableObject
{
    public DateTime Date { get; }
    public string DayText => Date.Day.ToString();
    public bool IsToday { get; }
    public string Emojis { get; }
    public IBrush Fill { get; }
    public double CellOpacity { get; }

    [ObservableProperty] private bool _isSelected;

    public CalendarDay(DateTime date, bool isInMonth, bool isToday, string emojis, double? emisiKg)
    {
        Date = date;
        IsToday = isToday;
        Emojis = emojis;
        CellOpacity = isInMonth ? 1.0 : 0.45;
        Fill = Brush.Parse(emisiKg switch
        {
            null => "#EAF4E4",          // belum ada catatan
            <= 0.5 => "#81C784",        // sangat hijau
            <= 2.0 => "#FFE082",        // sedang
            _ => "#FFAB91",             // tinggi
        });
    }
}
