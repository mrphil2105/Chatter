using System;
using System.Threading.Tasks;

namespace Chatter.ViewModels.Abstract
{
    public interface IDispatcher
    {
        Task InvokeAsync(Action action);

        Task InvokeAsync(Func<Task> func);
    }
}
