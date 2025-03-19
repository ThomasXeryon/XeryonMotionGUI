using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XeryonMotionGUI.ViewModels;

namespace XeryonMotionGUI.Views
{
    public sealed partial class IntroDialog : ContentDialog
    {
        private IntroPageViewModel ViewModel => DataContext as IntroPageViewModel;

        public IntroDialog()
        {
            this.InitializeComponent();
            DataContext = App.GetService<IntroPageViewModel>();
            this.Closing += IntroDialog_Closing; // Handle "Skip" or close
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (IntroSteps.SelectedIndex < IntroSteps.Items.Count - 1)
            {
                IntroSteps.SelectedIndex++; // Move to next step
            }
            else
            {
                FinishIntro(); // Last step, close dialog
            }

            // Update button text on the last step
            if (IntroSteps.SelectedIndex == IntroSteps.Items.Count - 1)
            {
                NextButton.Content = "Finish";
            }
        }

        private void IntroDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            // Handle "Skip" or dialog close
            FinishIntro();
        }

        private void FinishIntro()
        {
            // Save "don’t show again" preference if checked
            if (DontShowAgainCheckBox.IsChecked == true)
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["ShowIntro"] = false;
            }

            // Hide the dialog
            this.Hide();
        }
    }
}