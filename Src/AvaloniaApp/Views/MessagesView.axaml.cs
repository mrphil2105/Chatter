using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chatter.AvaloniaApp.Views
{
    public class MessagesView : UserControl
    {
        public MessagesView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
