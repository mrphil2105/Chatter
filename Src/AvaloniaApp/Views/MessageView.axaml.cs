using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chatter.AvaloniaApp.Views
{
    public class MessageView : UserControl
    {
        public MessageView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
