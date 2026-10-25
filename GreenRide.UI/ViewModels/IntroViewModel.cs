using CommunityToolkit.Mvvm.Input;

namespace GreenRide.UI.ViewModels;

public partial class IntroViewModel : ViewModelBase
{
    private readonly Action _onStart;
    public IntroViewModel(Action onStart) => _onStart = onStart;

    [RelayCommand] private void Mulai() => _onStart();
}
