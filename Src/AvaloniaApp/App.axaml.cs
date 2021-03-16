using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chatter.AvaloniaApp.Views;
using Chatter.ViewModels;

namespace Chatter.AvaloniaApp
{
    public class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.MainWindow = new MainWindow { DataContext = new MainViewModel() };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
