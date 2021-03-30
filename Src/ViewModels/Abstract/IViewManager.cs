using System.Threading.Tasks;

namespace Chatter.ViewModels.Abstract
{
    public interface IViewManager
    {
        Task ShowErrorBoxAsync(string message, string caption);
    }
}
