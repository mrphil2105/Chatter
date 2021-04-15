using System;

namespace Chatter.ViewModels.Commands
{
    public class RelayCommand : CommandBase
    {
        private readonly Func<bool>? _canExecuteFunc;
        private readonly Action _executeAction;

        public RelayCommand(Action executeAction, Func<bool>? canExecuteFunc = null)
        {
            _canExecuteFunc = canExecuteFunc;
            _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
        }

        protected override bool CanExecute()
        {
            return _canExecuteFunc?.Invoke() ?? true;
        }

        protected override void Execute()
        {
            _executeAction();
        }
    }
}
