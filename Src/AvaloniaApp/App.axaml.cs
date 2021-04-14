using System.Diagnostics;
using Autofac;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Chatter.Application.Services;
using Chatter.AvaloniaApp.Threading;
using Chatter.AvaloniaApp.Views;
using Chatter.ViewModels;
using Chatter.ViewModels.Abstract;
using IDispatcher = Chatter.ViewModels.Abstract.IDispatcher;

namespace Chatter.AvaloniaApp
{
    public class App : Avalonia.Application
    {
        private IContainer? _container;

        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(typeof(IServerService).Assembly)
                .Where(t => t.IsClass && !t.IsAbstract && t.IsInNamespace(typeof(IServerService).Namespace!))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(typeof(MainViewModel).Assembly)
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("ViewModel"));

            builder.RegisterType<ViewManager>()
                .As<IViewManager>()
                .SingleInstance();

            builder.Register(_ => new DispatcherWrapper(Dispatcher.UIThread))
                .As<IDispatcher>()
                .SingleInstance();

            _container = builder.Build();

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                Debug.Assert(_container != null, "IoC container was unexpectedly 'null'.");

                var mainViewModel = _container.Resolve<MainViewModel>();
                desktopLifetime.MainWindow = new MainWindow { DataContext = mainViewModel };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
