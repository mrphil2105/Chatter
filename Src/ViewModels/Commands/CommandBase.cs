using System;
using System.Windows.Input;

namespace Chatter.ViewModels.Commands
{
    public abstract class CommandBase : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        bool ICommand.CanExecute(object? parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object? parameter)
        {
            Execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract bool CanExecute();

        protected abstract void Execute();
    }
}
