using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Chatter.ViewModels;

namespace Chatter.AvaloniaApp.Views
{
    public class ViewLocator : IDataTemplate
    {
        private static readonly IReadOnlyList<Type> _viewTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("View"))
            .ToList();

        public IControl Build(object viewModel)
        {
            string viewModelName = viewModel.GetType()
                .Name;

            if (!viewModelName.EndsWith("ViewModel"))
            {
                throw new ArgumentException("View model type must end with suffix 'ViewModel'.", nameof(viewModel));
            }

            int removeIndex = viewModelName.Length - "Model".Length;
            string viewName = viewModelName.Remove(removeIndex);

            var viewType = _viewTypes.SingleOrDefault(t => t.Name == viewName);

            if (viewType != null)
            {
                return (Control)Activator.CreateInstance(viewType)!;
            }

            throw new InvalidOperationException($"View with name '{viewName}' could not be found.");
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}
