using CommunityToolkit.Mvvm.ComponentModel;
using XeryonMotionGUI.Contracts.Services;

namespace XeryonMotionGUI.ViewModels
{
    public class IntroPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public IntroPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public void NavigateToMainPage()
        {
            _navigationService.NavigateTo(typeof(MainViewModel).FullName);
        }
    }
}