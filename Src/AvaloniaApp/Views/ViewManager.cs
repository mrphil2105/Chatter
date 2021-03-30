using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Chatter.ViewModels.Abstract;

namespace Chatter.AvaloniaApp.Views
{
    public class ViewManager : IViewManager
    {
        public Task ShowErrorBoxAsync(string message, string caption)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (caption is null)
            {
                throw new ArgumentNullException(nameof(caption));
            }

            var mainWindow = GetMainWindow();
            var window = new ErrorBoxWindow { Title = caption };

            var messageTextBlock = window.FindControl<TextBlock>("MessageTextBlock");
            messageTextBlock.Text = message;

            var closeButton = window.FindControl<Button>("CloseButton");
            closeButton.Click += (_, _) => window.Close();

            return window.ShowDialog(mainWindow);
        }

        private static Window GetMainWindow()
        {
            return ((IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current.ApplicationLifetime)
                .MainWindow;
        }
    }
}
