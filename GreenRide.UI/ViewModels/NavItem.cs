using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GreenRide.UI.ViewModels;

public partial class NavItem : ObservableObject
{
    public string Title { get; }
    public string Emoji { get; }
    public PageViewModelBase Page { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty] private bool _isSelected;

    public NavItem(string title, string emoji, PageViewModelBase page, Action<NavItem> onSelect)
    {
        Title = title;
        Emoji = emoji;
        Page = page;
        SelectCommand = new RelayCommand(() => onSelect(this));
    }
}
