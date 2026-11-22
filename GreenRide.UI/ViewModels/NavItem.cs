using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GreenRide.UI.ViewModels;

/// <summary>Satu baris menu di sidebar.</summary>
public sealed partial class NavItem : ObservableObject
{
    public NavItem(string title, Geometry? icon, Action onClick)
    {
        Title = title;
        Icon = icon;
        Command = new RelayCommand(onClick);
    }

    public string Title { get; }
    public Geometry? Icon { get; }
    public ICommand Command { get; }

    [ObservableProperty] private bool _isActive;
}
