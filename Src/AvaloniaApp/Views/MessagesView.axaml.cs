using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Chatter.AvaloniaApp.Views
{
    public class MessagesView : UserControl
    {
        private bool _canAutoScroll;

        public MessagesView()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;

            // Only auto scroll if enabled and if the element has grown in height.
            if (_canAutoScroll && e.ExtentDelta.Y > 0)
            {
                scrollViewer.ScrollToEnd();
            }

            // Check if the 'ScrollViewer' is scrolled to the bottom and enable auto scrolling if that is the case.
            _canAutoScroll =
                Math.Abs(scrollViewer.Extent.Height - (scrollViewer.Offset.Y + scrollViewer.Bounds.Height)) < 0.01;
        }
    }
}
