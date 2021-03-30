using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chatter.AvaloniaApp.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDevTools();
#endif
        }
    }
}
