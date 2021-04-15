using System;
using System.Threading.Tasks;

namespace Chatter.ViewModels.Commands
{
    public class AsyncCommand : CommandBase
    {
        private readonly Func<bool>? _canExecuteFunc;
        private readonly Func<Task> _executeFunc;

        private bool _isExecuting;

        public AsyncCommand(Func<Task> executeFunc, Func<bool>? canExecuteFunc = null)
        {
            _canExecuteFunc = canExecuteFunc;
            _executeFunc = executeFunc ?? throw new ArgumentNullException(nameof(executeFunc));
        }

        protected override bool CanExecute()
        {
            return !_isExecuting && (_canExecuteFunc?.Invoke() ?? true);
        }

        protected override async void Execute()
        {
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _executeFunc();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
}
