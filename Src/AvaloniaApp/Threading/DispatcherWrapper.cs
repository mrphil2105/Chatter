using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using IDispatcher = Chatter.ViewModels.Abstract.IDispatcher;

namespace Chatter.AvaloniaApp.Threading
{
    public class DispatcherWrapper : IDispatcher
    {
        private readonly Dispatcher _dispatcher;

        public DispatcherWrapper(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public Task InvokeAsync(Action action)
        {
            return _dispatcher.InvokeAsync(action);
        }

        public Task InvokeAsync(Func<Task> func)
        {
            return _dispatcher.InvokeAsync(func);
        }
    }
}
