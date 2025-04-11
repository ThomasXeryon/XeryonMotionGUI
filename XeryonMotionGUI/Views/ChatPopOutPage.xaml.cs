using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI; // For Microsoft.UI.Colors
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace XeryonMotionGUI.Views
{
    public sealed partial class ChatPopOutPage : Page
    {
        private readonly ObservableCollection<ChatMessage> _chatMessages;
        private readonly Func<string, Task<string>> _sendMessageToCustomGpt;

        public ChatPopOutPage(ObservableCollection<ChatMessage> chatMessages, Func<string, Task<string>> sendMessageToCustomGpt)
        {
            _chatMessages = chatMessages;
            _sendMessageToCustomGpt = sendMessageToCustomGpt;
            InitializeComponent();

            ChatMessagesPanel.DataContext = _chatMessages;
            _chatMessages.CollectionChanged += (s, e) => UpdateChatUI();
            UpdateChatUI();
        }

        private void UpdateChatUI()
        {
            ChatMessagesPanel.Children.Clear();
            foreach (var message in _chatMessages)
            {
                var messageContainer = new Border
                {
                    Background = message.IsUser
                        ? new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColorLight2"])
                        : new SolidColorBrush((Color)Application.Current.Resources["SystemChromeMediumLowColor"]),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8),
                    Margin = new Thickness(4),
                    MaxWidth = 400, // Larger width for pop-out window
                    HorizontalAlignment = message.IsUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    BorderThickness = new Thickness(0)
                };

                var textBlock = new TextBlock
                {
                    Text = message.Content,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush((Color)Application.Current.Resources["SystemBaseHighColor"])
                };

                messageContainer.Child = textBlock;
                ChatMessagesPanel.Children.Add(messageContainer);
            }

            // Scroll to the bottom
            ChatScrollViewer?.ChangeView(null, ChatScrollViewer?.ExtentHeight, null, false);
        }

        private async void SendChatMessage_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void ChatInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                await SendMessage();
                e.Handled = true;
            }
        }

        private async Task SendMessage()
        {
            string userInput = ChatInputBox.Text.Trim();
            if (string.IsNullOrEmpty(userInput))
                return;

            // Add user's message to history
            _chatMessages.Add(new ChatMessage { IsUser = true, Content = userInput });
            ChatInputBox.Text = ""; // Clear input

            // Show loading animation
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;

            try
            {
                // Send to CustomGPT and get response
                string gptResponse = await _sendMessageToCustomGpt(userInput);
                if (gptResponse.StartsWith("[Error:"))
                {
                    _chatMessages.Add(new ChatMessage { IsUser = false, Content = gptResponse });
                }
                else
                {
                    _chatMessages.Add(new ChatMessage { IsUser = false, Content = gptResponse });
                }
            }
            finally
            {
                // Hide loading animation
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
            }
        }
    }
}