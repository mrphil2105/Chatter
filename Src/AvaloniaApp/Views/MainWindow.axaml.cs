using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chatter.AvaloniaApp.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
