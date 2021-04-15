using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Chatter.ViewModels.Commands;

namespace Chatter.UnitTests.ViewModels
{
    // An extensions class that makes it easier to call execute and can execute on commands.
    public static class CommandExtensions
    {
        public static bool CanExecute(this ICommand command)
        {
            return command.CanExecute(null);
        }

        public static void Execute(this RelayCommand command)
        {
            ((ICommand)command).Execute(null);
        }

        public static Task ExecuteAsync(this AsyncCommand command)
        {
            // Not a particularly elegant way to execute the 'AsyncCommand' in a way that gives us the task.
            // But the field should never be renamed anyway, so it should not matter.

            var executeFuncField =
                typeof(AsyncCommand).GetField("_executeFunc", BindingFlags.Instance | BindingFlags.NonPublic);
            var executeFunc = (Func<Task>)executeFuncField!.GetValue(command)!;

            return executeFunc();
        }
    }
}
